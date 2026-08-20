using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;
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
            public int? OrganizationId { get; set; }
            public int? BranchId { get; set; }
        }

        public record OrganizationDto(int Id, string Code, string Name);
        public record BranchDto(int Id, string Code, string Name, int ClinicId, string ClinicName, int LaboratoryId, string LaboratoryName);

        [HttpGet("organizations")]
        public async Task<ActionResult<IReadOnlyCollection<OrganizationDto>>> Organizations()
        {
            var orgs = await _db.Organizations
                .AsNoTracking()
                .Where(o => o.IsActive)
                .OrderBy(o => o.Name)
                .Select(o => new OrganizationDto(o.Id, o.Code, o.Name))
                .ToListAsync();

            return Ok(orgs);
        }

        [HttpGet("branches")]
        public async Task<ActionResult<IReadOnlyCollection<BranchDto>>> Branches([FromQuery] int organizationId)
        {
            var orgExists = await _db.Organizations
                .AsNoTracking()
                .AnyAsync(o => o.Id == organizationId && o.IsActive);
            if (!orgExists)
                return BadRequest("Invalid or inactive organization.");

            var branches = await _db.Branches
                .AsNoTracking()
                .Include(b => b.Clinic)
                .Include(b => b.Laboratory)
                .Where(b => b.IsActive && b.OrganizationId == organizationId)
                .OrderBy(b => b.Name)
                .Select(b => new BranchDto(b.Id, b.Code, b.Name, b.ClinicId, b.Clinic.Name, b.LaboratoryId, b.Laboratory.Name))
                .ToListAsync();

            return Ok(branches);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.UsernameOrEmail) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest("Username/email and password are required.");

            if (!req.OrganizationId.HasValue)
            {
                req.OrganizationId = await _db.Organizations
                    .AsNoTracking()
                    .Where(o => o.IsActive)
                    .OrderBy(o => o.Id)
                    .Select(o => (int?)o.Id)
                    .FirstOrDefaultAsync();
            }
            if (!req.OrganizationId.HasValue)
                return BadRequest("Organization is required.");

            var organization = await _db.Organizations
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == req.OrganizationId.Value && o.IsActive);
            if (organization == null)
                return BadRequest("Selected organization is invalid or inactive.");

            if (!req.BranchId.HasValue)
                return BadRequest("Branch is required.");

            var branch = await _db.Branches
                .AsNoTracking()
                .Include(b => b.Organization)
                .FirstOrDefaultAsync(b => b.Id == req.BranchId.Value && b.IsActive);
            if (branch == null)
                return BadRequest("Selected branch is invalid or inactive.");

            if (branch.OrganizationId != organization.Id)
                return BadRequest("Selected branch does not belong to the selected organization.");

            // Try username, then email
            var user = await _users.FindByNameAsync(req.UsernameOrEmail)
                       ?? await _users.FindByEmailAsync(req.UsernameOrEmail);

            if (user == null) return Unauthorized("Invalid credentials");

            if (!user.IsActive)
            {
                await RecordLoginAsync(user.Id, false, "User disabled");
                return Unauthorized("User disabled");
            }

            var pwCheck = await _signIn.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: false);
            if (!pwCheck.Succeeded)
            {
                await RecordLoginAsync(user.Id, false, "Invalid password");
                return Unauthorized("Invalid credentials");
            }

            // Check user branch access
            var roles = await _users.GetRolesAsync(user);
            var isAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase) ||
                          roles.Contains("SuperAdmin", StringComparer.OrdinalIgnoreCase);

            if (!isAdmin)
            {
                var hasAccess = await _db.UserLocations
                    .AsNoTracking()
                    .AnyAsync(ul => ul.UserId == user.Id && ul.BranchId == req.BranchId.Value);
                if (!hasAccess)
                {
                    await RecordLoginAsync(user.Id, false, "Branch access denied");
                    return Forbid("You do not have access to the selected branch.");
                }
            }

            // Get clinic and laboratory for the branch
            var clinic = await _db.Clinics.AsNoTracking().FirstOrDefaultAsync(c => c.Id == branch.ClinicId);
            var laboratory = await _db.Laboratories.AsNoTracking().FirstOrDefaultAsync(l => l.Id == branch.LaboratoryId);

            // Determine access flags
            bool hasClinicAccess, hasLaboratoryAccess;
            if (isAdmin)
            {
                hasClinicAccess = true;
                hasLaboratoryAccess = true;
            }
            else
            {
                var userLocation = await _db.UserLocations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ul => ul.UserId == user.Id && ul.BranchId == req.BranchId.Value);
                hasClinicAccess = userLocation?.HasClinicAccess ?? false;
                hasLaboratoryAccess = userLocation?.HasLaboratoryAccess ?? false;
            }

            // Create JWT
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.UserName ?? ""),
                new("organization_id", organization.Id.ToString()),
                new("organization_name", organization.Name ?? string.Empty),
                new("laboratory_id", laboratory?.Id.ToString() ?? ""),
                new("laboratory_name", laboratory?.Name ?? ""),
                new("branch_id", branch.Id.ToString()),
                new("branch_name", branch.Name),
                new("clinic_access", hasClinicAccess.ToString().ToLower()),
                new("laboratory_access", hasLaboratoryAccess.ToString().ToLower())
            };

            // Include all assigned branches for this user within the organization
            var assignedBranchIds = await _db.UserLocations
                .AsNoTracking()
                .Where(ul => ul.UserId == user.Id)
                .Join(_db.Branches, ul => ul.BranchId, b => b.Id, (ul, b) => new { ul, b })
                .Where(x => x.b.OrganizationId == organization.Id)
                .Select(x => x.ul.BranchId)
                .ToListAsync();

            // Admin gets all branches in the org
            if (isAdmin)
            {
                assignedBranchIds = await _db.Branches
                    .AsNoTracking()
                    .Where(b => b.IsActive && b.OrganizationId == organization.Id)
                    .Select(b => b.Id)
                    .ToListAsync();
            }

            if (assignedBranchIds.Count > 0)
            {
                var csv = string.Join(',', assignedBranchIds);
                claims.Add(new Claim("branch_ids", csv));
            }

            // Add roles
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

            await RecordLoginAsync(user.Id, true);

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                organizationId = organization.Id,
                branchId = branch.Id
            });
        }

        private async Task RecordLoginAsync(int userId, bool success, string? failureReason = null)
        {
            try
            {
                _db.LoginHistories.Add(new LoginHistory
                {
                    UserId = userId,
                    Success = success,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    FailureReason = failureReason
                });
                await _db.SaveChangesAsync();
            }
            catch
            {
                // Login auditing must never break authentication.
            }
        }

    }
}
