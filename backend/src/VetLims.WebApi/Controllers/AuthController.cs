using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;
using VetLims.Infrastructure.Identity;

namespace VetLims.WebApi.Controllers
{
    /// <summary>
    /// Two-step login:
    ///   1. POST /login (credentials only) -> organization is derived from the user,
    ///      authorized ACTIVE branches are returned together with a short-lived
    ///      context token that carries NO branch context.
    ///   2. POST /select-branch -> validates the branch against the user's explicit
    ///      access list and issues the full working token.
    /// GET /my-branches returns the currently authorized branches for the header switcher.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AuthController : ControllerBase
    {
        private const string BranchSelectionContext = "branch-selection";
        private static readonly TimeSpan ContextTokenLifetime = TimeSpan.FromMinutes(10);

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

        public record OrganizationDto(int Id, string Code, string Name);
        public record AuthorizedBranchDto(
            int Id,
            string Name,
            string LaboratoryName,
            string ClinicName,
            bool IsDefault,
            bool HasClinicAccess,
            bool HasLaboratoryAccess);
        public record LoginRequest(string UsernameOrEmail, string Password);
        public record SelectBranchRequest(int BranchId);
        public record SelectBranchResponse(
            string Token,
            int OrganizationId,
            string OrganizationName,
            AuthorizedBranchDto Branch);

        // ------------------------------------------------------------------
        // STEP 1 — authenticate credentials, resolve organization + branches
        // ------------------------------------------------------------------
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.UsernameOrEmail) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest("Username/email and password are required.");

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

            // Organization is determined from the persisted user, never from the request.
            var organization = await ResolveUserOrganizationAsync(user);
            if (organization == null)
            {
                await RecordLoginAsync(user.Id, false, "Organization missing or inactive");
                return Forbidden("User is not assigned to an active organization.");
            }

            var roles = await _users.GetRolesAsync(user);

            var branches = await LoadAuthorizedBranchesAsync(user, roles);
            if (branches.Count == 0)
            {
                await RecordLoginAsync(user.Id, false, "No active branch access");
                return Forbidden("You do not have access to any active branch.");
            }

            await RecordLoginAsync(user.Id, true);

            return Ok(new
            {
                organization = new { id = organization.Id, name = organization.Name },
                branches,
                requiresBranchSelection = branches.Count > 1,
                contextToken = CreateToken(user.Id, user.UserName ?? "", user.DisplayName, organization, roles, branch: null),
            });
        }

        // ------------------------------------------------------------------
        // STEP 2 — establish the Active Branch working context
        // Accepts either the step-1 context token or an existing full token
        // (the latter makes this endpoint the secure Branch Switcher).
        // ------------------------------------------------------------------
        [HttpPost("select-branch")]
        public async Task<ActionResult<SelectBranchResponse>> SelectBranch([FromBody] SelectBranchRequest req)
        {
            var auth = await ResolveAuthenticatedUserAsync();
            if (!auth.IsSuccess) return auth.Error!;
            var user = auth.User!;
            var organization = auth.Organization!;
            var roles = auth.Roles!;

            var branch = await _db.Branches.AsNoTracking()
                .Include(b => b.Clinic)
                .Include(b => b.Laboratory)
                .FirstOrDefaultAsync(b => b.Id == req.BranchId && b.IsActive && !b.IsDeleted);

            if (branch == null || branch.OrganizationId != organization.Id)
                return Forbidden("Selected branch is invalid for your organization.");

            var isAdmin = IsAdmin(roles);
            bool clinicAccess;
            bool laboratoryAccess;

            if (isAdmin)
            {
                clinicAccess = true;
                laboratoryAccess = true;
            }
            else
            {
                var location = await _db.UserLocations.AsNoTracking()
                    .FirstOrDefaultAsync(ul => ul.UserId == user.Id
                                               && ul.BranchId == branch.Id
                                               && ul.IsActive
                                               && !ul.IsDeleted);
                if (location == null)
                {
                    await RecordLoginAsync(user.Id, false, "Branch access denied");
                    return Forbidden("You do not have access to the selected branch.");
                }

                clinicAccess = location.HasClinicAccess;
                laboratoryAccess = location.HasLaboratoryAccess;
            }

            var assignedBranchIds = await LoadAuthorizedBranchIdsAsync(user, roles);

            var token = CreateToken(
                user.Id, user.UserName ?? "", user.DisplayName, organization, roles,
                branch: branch, clinicAccess: clinicAccess, laboratoryAccess: laboratoryAccess,
                assignedBranchIds: assignedBranchIds);

            return Ok(new SelectBranchResponse(
                token,
                organization.Id,
                organization.Name,
                new AuthorizedBranchDto(
                    branch.Id,
                    branch.Name,
                    branch.Laboratory?.Name ?? string.Empty,
                    branch.Clinic?.Name ?? string.Empty,
                    IsDefault: false,
                    clinicAccess,
                    laboratoryAccess)));
        }

        // ------------------------------------------------------------------
        // Authorized branch list for the header switcher / selection screen.
        // Always recomputed from the database; client-supplied ids are ignored.
        // ------------------------------------------------------------------
        [HttpGet("my-branches")]
        public async Task<IActionResult> MyBranches()
        {
            var auth = await ResolveAuthenticatedUserAsync();
            if (!auth.IsSuccess) return auth.Error!;
            var user = auth.User!;
            var organization = auth.Organization!;
            var roles = auth.Roles!;

            var branches = await LoadAuthorizedBranchesAsync(user, roles);
            return Ok(new { organization = new { id = organization.Id, name = organization.Name }, branches });
        }

        // ==================================================================

        // ControllerBase.Forbid(string) treats its argument as an authentication
        // scheme name, not a message; a plain 403 object result is used instead.
        private ActionResult Forbidden(string message) =>
            StatusCode(StatusCodes.Status403Forbidden, new { message });

        private async Task<Organization?> ResolveUserOrganizationAsync(AppUser user)
        {
            if (!user.OrganizationId.HasValue) return null;

            return await _db.Organizations.AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == user.OrganizationId.Value && o.IsActive && !o.IsDeleted);
        }

        private bool IsAdmin(IList<string> roles) =>
            roles.Contains("Admin", StringComparer.OrdinalIgnoreCase) ||
            roles.Contains("SuperAdmin", StringComparer.OrdinalIgnoreCase);

        private sealed record AuthContextResult(
            AppUser? User, Organization? Organization, IList<string>? Roles, ActionResult? Error)
        {
            public bool IsSuccess => Error == null;
        }

        private async Task<AuthContextResult> ResolveAuthenticatedUserAsync()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
                return new AuthContextResult(null, null, null, Unauthorized());

            var user = await _users.FindByIdAsync(userId.ToString());
            if (user == null || !user.IsActive)
                return new AuthContextResult(null, null, null, Unauthorized("Invalid user."));

            var organization = await ResolveUserOrganizationAsync(user);
            if (organization == null)
                return new AuthContextResult(null, null, null,
                    Forbidden("User is not assigned to an active organization."));

            var roles = await _users.GetRolesAsync(user);
            return new AuthContextResult(user, organization, roles, null);
        }

        private async Task<List<AuthorizedBranchDto>> LoadAuthorizedBranchesAsync(AppUser user, IList<string> roles)
        {
            if (!user.OrganizationId.HasValue) return new List<AuthorizedBranchDto>();
            var orgId = user.OrganizationId.Value;

            var branchQuery = _db.Branches.AsNoTracking()
                .Where(b => b.IsActive && !b.IsDeleted && b.OrganizationId == orgId);

            if (IsAdmin(roles))
            {
                return await branchQuery
                    .OrderBy(b => b.Name)
                    .Select(b => new AuthorizedBranchDto(
                        b.Id, b.Name, b.Laboratory.Name, b.Clinic.Name,
                        false, true, true))
                    .ToListAsync();
            }

            return await (
                from ul in _db.UserLocations.AsNoTracking()
                join b in branchQuery on ul.BranchId equals b.Id
                where ul.UserId == user.Id && ul.IsActive && !ul.IsDeleted
                orderby ul.IsDefault descending, b.Name
                select new AuthorizedBranchDto(
                    b.Id, b.Name, b.Laboratory.Name, b.Clinic.Name,
                    ul.IsDefault, ul.HasClinicAccess, ul.HasLaboratoryAccess))
                .ToListAsync();
        }

        private async Task<List<int>> LoadAuthorizedBranchIdsAsync(AppUser user, IList<string> roles)
        {
            if (!user.OrganizationId.HasValue) return new List<int>();
            var orgId = user.OrganizationId.Value;

            if (IsAdmin(roles))
            {
                return await _db.Branches.AsNoTracking()
                    .Where(b => b.IsActive && !b.IsDeleted && b.OrganizationId == orgId)
                    .Select(b => b.Id)
                    .ToListAsync();
            }

            return await (
                from ul in _db.UserLocations.AsNoTracking()
                join b in _db.Branches.AsNoTracking() on ul.BranchId equals b.Id
                where ul.UserId == user.Id && ul.IsActive && !ul.IsDeleted
                      && b.IsActive && !b.IsDeleted && b.OrganizationId == orgId
                select ul.BranchId)
                .Distinct()
                .ToListAsync();
        }

        private string CreateToken(
            int userId,
            string userName,
            string? displayName,
            Organization organization,
            IList<string> roles,
            Branch? branch,
            bool clinicAccess = false,
            bool laboratoryAccess = false,
            IReadOnlyCollection<int>? assignedBranchIds = null)
        {
            var claims = new List<Claim>
            {
                // Short JWT claim names ("nameid", "unique_name", "role") keep the
                // payload keys stable across System.IdentityModel versions; the JWT
                // bearer's inbound claim mapping resolves them back to ClaimTypes.*
                // so [Authorize(Roles=...)] and policies keep working.
                new("nameid", userId.ToString()),
                new("unique_name", userName),
                new("display_name", string.IsNullOrWhiteSpace(displayName) ? userName : displayName),
                new("organization_id", organization.Id.ToString()),
                new("organization_name", organization.Name ?? string.Empty),
            };

            if (branch == null)
            {
                // Context-only token: authentication works, but every data API is
                // blocked by the fallback policy until a branch has been selected.
                claims.Add(new Claim("ctx", BranchSelectionContext));
            }
            else
            {
                claims.Add(new Claim("laboratory_id", branch.LaboratoryId.ToString()));
                claims.Add(new Claim("laboratory_name", branch.Laboratory?.Name ?? ""));
                claims.Add(new Claim("branch_id", branch.Id.ToString()));
                claims.Add(new Claim("branch_name", branch.Name));
                claims.Add(new Claim("clinic_access", clinicAccess.ToString().ToLowerInvariant()));
                claims.Add(new Claim("laboratory_access", laboratoryAccess.ToString().ToLowerInvariant()));

                if (assignedBranchIds is { Count: > 0 })
                    claims.Add(new Claim("branch_ids", string.Join(',', assignedBranchIds)));
            }

            foreach (var role in roles)
                claims.Add(new Claim("role", role));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"] ?? "dev-key-change-me"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expires = branch == null
                ? DateTime.UtcNow.Add(ContextTokenLifetime)
                : DateTime.UtcNow.AddHours(8);

            var token = new JwtSecurityToken(
                issuer: _cfg["Jwt:Issuer"] ?? "VetLims",
                audience: _cfg["Jwt:Audience"] ?? "VetLimsClients",
                claims: claims,
                expires: expires,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
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
