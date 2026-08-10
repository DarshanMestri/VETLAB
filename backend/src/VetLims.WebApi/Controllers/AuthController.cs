using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Identity;

namespace VetLims.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _users;
        private readonly SignInManager<AppUser> _signIn;
        private readonly IConfiguration _cfg;
        private readonly VetLimsDbContext _db;

        public AuthController(UserManager<AppUser> users, SignInManager<AppUser> signIn, IConfiguration cfg, VetLimsDbContext db)
        {
            _users = users;
            _signIn = signIn;
            _cfg = cfg;
            _db = db;
        }

        public class LoginRequest
        {
            public string UsernameOrEmail { get; set; } = "";
            public string Password { get; set; } = "";
            public int? ClinicId { get; set; }
            public int? BranchId { get; set; }
        }

        public record LabLocationBranchDto(int BranchId, string BranchCode, string BranchName);
        public record LabLocationClinicDto(int ClinicId, string ClinicCode, string ClinicName, IReadOnlyCollection<LabLocationBranchDto> Branches);

        [HttpGet("lab-locations")]
        public async Task<ActionResult<IReadOnlyCollection<LabLocationClinicDto>>> LabLocations()
        {
            await EnsureDefaultLocationHierarchyAsync();

            var clinics = await _db.Clinics
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Code, c.Name })
                .ToListAsync();

            var branches = await _db.Branches
                .AsNoTracking()
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .Select(b => new { b.Id, b.Code, b.Name, b.ClinicId })
                .ToListAsync();

            var branchLookup = branches
                .GroupBy(b => b.ClinicId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyCollection<LabLocationBranchDto>)g
                        .Select(x => new LabLocationBranchDto(x.Id, x.Code, x.Name))
                        .ToList());

            var response = clinics
                .Select(c => new LabLocationClinicDto(
                    c.Id,
                    c.Code,
                    c.Name,
                    branchLookup.TryGetValue(c.Id, out var clinicBranches)
                        ? clinicBranches
                        : Array.Empty<LabLocationBranchDto>()))
                .ToList();

            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.UsernameOrEmail) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest("Username/email and password are required.");

            await EnsureDefaultLocationHierarchyAsync();

            if (!req.ClinicId.HasValue)
            {
                req.ClinicId = await _db.Clinics
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Id)
                    .Select(c => (int?)c.Id)
                    .FirstOrDefaultAsync();
            }
            if (!req.ClinicId.HasValue)
                return BadRequest("Lab location is required.");

            var clinic = await _db.Clinics.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == req.ClinicId.Value && c.IsActive);
            if (clinic == null)
                return BadRequest("Selected lab location is invalid or inactive.");

            var activeBranches = await _db.Branches.AsNoTracking()
                .Where(b => b.IsActive && b.ClinicId == clinic.Id)
                .OrderBy(b => b.Name)
                .ToListAsync();

            if (activeBranches.Count == 1 && !req.BranchId.HasValue)
                req.BranchId = activeBranches[0].Id;

            if (activeBranches.Count > 0 && !req.BranchId.HasValue)
                return BadRequest("Branch is required for the selected lab location.");

            var branch = req.BranchId.HasValue
                ? activeBranches.FirstOrDefault(b => b.Id == req.BranchId.Value)
                : null;

            if (req.BranchId.HasValue && branch == null)
                return BadRequest("Selected branch is invalid or inactive.");

            // Try username, then email
            var user = await _users.FindByNameAsync(req.UsernameOrEmail)
                       ?? await _users.FindByEmailAsync(req.UsernameOrEmail);

            if (user == null) return Unauthorized("Invalid credentials");

            // Optional: if you have IsActive on AppUser
            var isActiveProp = user.GetType().GetProperty("IsActive");
            if (isActiveProp != null && isActiveProp.GetValue(user) is bool active && !active)
                return Unauthorized("User disabled");

            var pwCheck = await _signIn.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: false);
            if (!pwCheck.Succeeded) return Unauthorized("Invalid credentials");

            // Create JWT
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.UserName ?? ""),
                new("organization_id", clinic.OrganizationId.ToString()),
                new("organization_name", clinic.Organization.Name ?? string.Empty),
                new("clinic_id", clinic.Id.ToString()),
                new("clinic_name", clinic.Name)
            };
            if (branch != null)
            {
                claims.Add(new Claim("branch_id", branch.Id.ToString()));
                claims.Add(new Claim("branch_name", branch.Name));
            }

            // If user has multiple assigned branches, include as comma-separated claim
            var assignedBranches = await _db.UserLocations.AsNoTracking().Where(ul => ul.UserId == user.Id).Select(ul => ul.BranchId).ToListAsync();
            if (assignedBranches.Count > 0)
            {
                var csv = string.Join(',', assignedBranches);
                claims.Add(new Claim("branch_ids", csv));

                // If no branch selected but user has a default, include it
                if (!claims.Any(c => c.Type == "branch_id"))
                {
                    var defaultBranch = await _db.UserLocations.AsNoTracking().Where(ul => ul.UserId == user.Id && ul.IsDefault).Select(ul => (int?)ul.BranchId).FirstOrDefaultAsync();
                    if (defaultBranch.HasValue) claims.Add(new Claim("branch_id", defaultBranch.Value.ToString()));
                }
            }

            // Add roles
            var roles = await _users.GetRolesAsync(user);
            foreach (var r in roles) claims.Add(new Claim(ClaimTypes.Role, r));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"] ?? "dev-key-change-me"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _cfg["Jwt:Issuer"] ?? "VetLims",
                audience: _cfg["Jwt:Audience"] ?? "VetLimsClients",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                clinicId = clinic.Id,
                branchId = branch?.Id
            });
        }

        private async Task EnsureDefaultLocationHierarchyAsync()
        {
            var hasActiveClinic = await _db.Clinics.AsNoTracking().AnyAsync(c => c.IsActive);
            if (hasActiveClinic) return;

            var organization = await _db.Organizations.FirstOrDefaultAsync(x => x.IsActive);
            if (organization == null)
            {
                organization = new Infrastructure.Entities.Organization
                {
                    Code = "DEFAULT",
                    Name = "Default Organization",
                    IsActive = true
                };
                _db.Organizations.Add(organization);
                await _db.SaveChangesAsync();
            }

            var clinic = new Infrastructure.Entities.Clinic
            {
                OrganizationId = organization.Id,
                Code = "MAINLAB",
                Name = "Main Laboratory",
                IsActive = true
            };
            _db.Clinics.Add(clinic);
            await _db.SaveChangesAsync();

            var branch = new Infrastructure.Entities.Branch
            {
                ClinicId = clinic.Id,
                Code = "MAIN",
                Name = "Main Location",
                IsActive = true
            };
            _db.Branches.Add(branch);
            await _db.SaveChangesAsync();
        }
    }
}
