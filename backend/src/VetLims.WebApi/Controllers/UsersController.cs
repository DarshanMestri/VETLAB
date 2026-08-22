// Step 4: Admin-only user ops
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Identity;

namespace VetLims.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireAdmin")]
public class UsersController : ControllerBase
{
    private readonly UserManager<AppUser> _users;
    private readonly RoleManager<IdentityRole<int>> _roles;
    private readonly VetLimsDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UsersController(
        UserManager<AppUser> users,
        RoleManager<IdentityRole<int>> roles,
        VetLimsDbContext db,
        ICurrentUserService currentUser)
    {
        _users = users;
        _roles = roles;
        _db = db;
        _currentUser = currentUser;
    }

    public record RoleRequest(string RoleName);
    public record ToggleActiveRequest(bool IsActive);
    public record CreateUserRequest(
        string UserName,
        string Email,
        string DisplayName,
        string Password,
        string RoleName,
        int OrganizationId,
        bool IsActive = true);
    public record UpdateUserRequest(
        string UserName,
        string Email,
        string DisplayName,
        string RoleName,
        int OrganizationId,
        bool IsActive,
        string? NewPassword = null);

    public record LoginHistoryItem(
        int Id,
        int UserId,
        string? UserName,
        string? DisplayName,
        bool Success,
        string? IpAddress,
        string? UserAgent,
        string? FailureReason,
        DateTime CreatedAt);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> List()
    {
        var users = await _users.Users
            .AsNoTracking()
            .OrderBy(u => u.UserName)
            .ToListAsync();

        var data = new List<object>();
        foreach (var user in users)
        {
            data.Add(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.DisplayName,
                user.OrganizationId,
                user.BranchId,
                user.IsActive,
                Roles = await _users.GetRolesAsync(user)
            });
        }

        return Ok(data);
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] CreateUserRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.UserName) || string.IsNullOrWhiteSpace(req.Email) ||
            string.IsNullOrWhiteSpace(req.DisplayName) || string.IsNullOrWhiteSpace(req.Password) ||
            string.IsNullOrWhiteSpace(req.RoleName))
            return BadRequest("Username, email, display name, password, and role are required.");

        var roleName = req.RoleName.Trim();
        if (!await _roles.RoleExistsAsync(roleName))
            return BadRequest("Selected role does not exist.");

        var organization = await _db.Organizations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.OrganizationId && x.IsActive);
        if (organization is null)
            return BadRequest("Selected organization is invalid or inactive.");

        // NOTE: branch access is no longer taken from the create payload; it is
        // granted explicitly via /api/users/{id}/locations (UserLocationsController).
        var user = new AppUser
        {
            UserName = req.UserName.Trim(),
            Email = req.Email.Trim(),
            DisplayName = req.DisplayName.Trim(),
            OrganizationId = req.OrganizationId,
            IsActive = req.IsActive,
            EmailConfirmed = true
        };

        var result = await _users.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(string.Join("; ", result.Errors.Select(e => e.Description)));

        var roleResult = await _users.AddToRoleAsync(user, roleName);
        if (!roleResult.Succeeded)
        {
            await _users.DeleteAsync(user);
            return BadRequest(string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        }

        return CreatedAtAction(nameof(List), new { id = user.Id }, new
        {
            user.Id, user.UserName, user.Email, user.DisplayName,
            user.OrganizationId, user.IsActive,
            Roles = new[] { roleName }
        });
    }

    [HttpPut("{userId:int}")]
    public async Task<ActionResult<object>> Update(int userId, [FromBody] UpdateUserRequest req)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user == null) return NotFound();

        if (string.IsNullOrWhiteSpace(req.UserName) || string.IsNullOrWhiteSpace(req.Email) ||
            string.IsNullOrWhiteSpace(req.DisplayName) || string.IsNullOrWhiteSpace(req.RoleName))
            return BadRequest("Username, email, display name, and role are required.");

        var roleName = req.RoleName.Trim();
        if (!await _roles.RoleExistsAsync(roleName))
            return BadRequest("Selected role does not exist.");

        var organization = await _db.Organizations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.OrganizationId && x.IsActive);
        if (organization is null)
            return BadRequest("Selected organization is invalid or inactive.");

        var newUserName = req.UserName.Trim();
        var newEmail = req.Email.Trim();

        if (!string.Equals(newUserName, user.UserName, StringComparison.OrdinalIgnoreCase) &&
            await _users.Users.AnyAsync(x => x.Id != userId && x.NormalizedUserName == newUserName.ToUpperInvariant()))
            return BadRequest("That username is already in use.");

        if (!string.Equals(newEmail, user.Email, StringComparison.OrdinalIgnoreCase) &&
            await _users.Users.AnyAsync(x => x.Id != userId && x.NormalizedEmail == newEmail.ToUpperInvariant()))
            return BadRequest("That email address is already in use.");

        // Safety: never let an admin lock themselves out or demote their own role.
        if (userId.ToString() == User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value)
        {
            var currentRoles = await _users.GetRolesAsync(user);
            if (currentRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase) &&
                !string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase))
                return BadRequest("You cannot remove your own administrator role.");
            if (!req.IsActive)
                return BadRequest("You cannot deactivate your own account.");
        }

        user.UserName = newUserName;
        user.Email = newEmail;
        user.DisplayName = req.DisplayName.Trim();
        user.OrganizationId = req.OrganizationId;
        // Branch access is managed exclusively through UserLocations; the legacy
        // single-branch column is no longer written.
        user.IsActive = req.IsActive;

        var result = await _users.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(string.Join("; ", result.Errors.Select(e => e.Description)));

        var existingRoles = await _users.GetRolesAsync(user);
        if (!existingRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var role in existingRoles)
                await _users.RemoveFromRoleAsync(user, role);

            var roleResult = await _users.AddToRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
                return BadRequest(string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        }

        if (!string.IsNullOrWhiteSpace(req.NewPassword))
        {
            var removeResult = await _users.RemovePasswordAsync(user);
            if (removeResult.Succeeded)
            {
                var addResult = await _users.AddPasswordAsync(user, req.NewPassword);
                if (!addResult.Succeeded)
                    return BadRequest(string.Join("; ", addResult.Errors.Select(e => e.Description)));
            }
        }

        return Ok(new
        {
            user.Id, user.UserName, user.Email, user.DisplayName,
            user.OrganizationId, user.IsActive,
            Roles = new[] { roleName }
        });
    }

    [HttpPost("{userId:int}/roles")]
    public async Task<ActionResult> AddRole(int userId, [FromBody] RoleRequest req)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user == null) return NotFound();

        var res = await _users.AddToRoleAsync(user, req.RoleName);
        if (!res.Succeeded) return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));
        return Ok();
    }

    [HttpDelete("{userId:int}/roles/{roleName}")]
    public async Task<ActionResult> RemoveRole(int userId, string roleName)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user == null) return NotFound();

        var res = await _users.RemoveFromRoleAsync(user, roleName);
        if (!res.Succeeded) return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));
        return Ok();
    }

    [HttpPatch("{userId:int}/active")]
    public async Task<ActionResult> ToggleActive(int userId, [FromBody] ToggleActiveRequest req)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user == null) return NotFound();

        user.IsActive = req.IsActive;
        var res = await _users.UpdateAsync(user);
        if (!res.Succeeded) return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));
        return Ok();
    }

    [HttpGet("hierarchy")]
    public async Task<ActionResult<object>> Hierarchy()
    {
        var organizationId = await ResolveOrganizationIdAsync();
        if (!organizationId.HasValue) return Forbid();

        var org = await _db.Organizations.AsNoTracking()
            .Where(x => x.Id == organizationId.Value && !x.IsDeleted && x.IsActive)
            .Select(x => new { x.Id, x.Code, x.Name })
            .FirstOrDefaultAsync();
        if (org is null) return NotFound("Organization not found.");

        var branches = await _db.Branches.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId.Value && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync();

        var users = await _users.Users.AsNoTracking().OrderBy(u => u.UserName).ToListAsync();
        var roleMap = await BuildRoleMapAsync();

        var lastLogins = await _db.LoginHistories.AsNoTracking()
            .Where(h => h.Success)
            .GroupBy(h => h.UserId)
            .Select(g => new { UserId = g.Key, At = g.Max(h => h.CreatedAt) })
            .ToDictionaryAsync(x => x.UserId, x => x.At);

        object UserNode(AppUser u) => new
        {
            u.Id,
            u.UserName,
            u.DisplayName,
            u.Email,
            u.IsActive,
            Roles = roleMap.TryGetValue(u.Id, out var r) ? r : new List<string>(),
            LastLoginAt = lastLogins.TryGetValue(u.Id, out var at) ? at : (DateTime?)null
        };

        // Group by explicitly granted branch access (UserLocation), not the legacy
        // single-branch column.
        var locationIndex = await _db.UserLocations.AsNoTracking()
            .Where(ul => !ul.IsDeleted)
            .Select(ul => new { ul.UserId, ul.BranchId })
            .ToListAsync();

        var branchNodes = branches.Select(b => new
        {
            b.Id,
            b.Code,
            b.Name,
            b.IsActive,
            Users = locationIndex
                .Where(l => l.BranchId == b.Id)
                .Join(users, l => l.UserId, u => u.Id, (_, u) => u)
                .Select(UserNode)
                .ToList()
        }).ToList();

        var assignedUserIds = locationIndex
            .Where(l => branches.Any(b => b.Id == l.BranchId))
            .Select(l => l.UserId)
            .ToHashSet();

        var unassigned = users
            .Where(u => !assignedUserIds.Contains(u.Id))
            .Select(UserNode)
            .ToList();

        return Ok(new { organization = org, branches = branchNodes, unassigned });
    }

    [HttpGet("login-history")]
    public async Task<ActionResult<IEnumerable<LoginHistoryItem>>> LoginHistory(
        [FromQuery] int limit = 100,
        [FromQuery] bool? success = null)
    {
        var organizationId = await ResolveOrganizationIdAsync();
        if (!organizationId.HasValue) return Forbid();

        if (limit is < 1 or > 500) return BadRequest("Limit must be between 1 and 500.");

        var q = _db.LoginHistories.AsNoTracking()
            .Where(h => h.User != null && h.User.OrganizationId == organizationId.Value);

        if (success.HasValue)
            q = q.Where(h => h.Success == success.Value);

        var items = await q
            .OrderByDescending(h => h.CreatedAt)
            .Take(limit)
            .Select(h => new LoginHistoryItem(
                h.Id,
                h.UserId,
                h.User!.UserName,
                h.User.DisplayName,
                h.Success,
                h.IpAddress,
                h.UserAgent,
                h.FailureReason,
                h.CreatedAt))
            .ToListAsync();

        return Ok(items);
    }

    private async Task<int?> ResolveOrganizationIdAsync()
    {
        var orgId = _currentUser.OrganizationId;
        if (!orgId.HasValue) return null;

        return await _db.Organizations.AsNoTracking()
            .Where(x => x.Id == orgId.Value && !x.IsDeleted && x.IsActive)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<Dictionary<int, List<string>>> BuildRoleMapAsync()
    {
        var rows = await (from ur in _db.UserRoles.AsNoTracking()
                          join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
                          select new { ur.UserId, RoleName = r.Name ?? string.Empty })
            .ToListAsync();

        return rows
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).ToList());
    }
}
