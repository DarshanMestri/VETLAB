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

    public UsersController(
        UserManager<AppUser> users,
        RoleManager<IdentityRole<int>> roles,
        VetLimsDbContext db)
    {
        _users = users;
        _roles = roles;
        _db = db;
    }

    public record RoleRequest(string RoleName);
    public record ToggleActiveRequest(bool IsActive);
    public record CreateUserRequest(
        string UserName,
        string Email,
        string DisplayName,
        string Password,
        string RoleName,
        int ClinicId,
        int? BranchId,
        bool IsActive = true);
    public record UpdateUserRequest(
        string UserName,
        string Email,
        string DisplayName,
        string RoleName,
        int ClinicId,
        int? BranchId,
        bool IsActive,
        string? NewPassword = null);

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
                user.ClinicId,
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

        var selectedClinic = await _db.Clinics.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.ClinicId && x.IsActive);
        if (selectedClinic is null || !selectedClinic.OrganizationId.HasValue)
            return BadRequest("Selected laboratory is invalid or has no organization.");

        if (req.BranchId.HasValue && !await _db.Branches.AsNoTracking()
                .AnyAsync(x => x.Id == req.BranchId.Value && x.ClinicId == req.ClinicId && x.IsActive))
            return BadRequest("Selected branch is invalid for the laboratory.");

        var user = new AppUser
        {
            UserName = req.UserName.Trim(),
            Email = req.Email.Trim(),
            DisplayName = req.DisplayName.Trim(),
            OrganizationId = selectedClinic.OrganizationId,
            ClinicId = req.ClinicId,
            BranchId = req.BranchId,
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
            user.OrganizationId, user.ClinicId, user.BranchId, user.IsActive,
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

        var selectedClinic = await _db.Clinics.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.ClinicId && x.IsActive);
        if (selectedClinic is null || !selectedClinic.OrganizationId.HasValue)
            return BadRequest("Selected laboratory is invalid or has no organization.");

        if (req.BranchId.HasValue && !await _db.Branches.AsNoTracking()
                .AnyAsync(x => x.Id == req.BranchId.Value && x.ClinicId == req.ClinicId && x.IsActive))
            return BadRequest("Selected branch is invalid for the laboratory.");

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
        user.ClinicId = req.ClinicId;
        user.BranchId = req.BranchId;
        user.OrganizationId = selectedClinic.OrganizationId;
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
            user.OrganizationId, user.ClinicId, user.BranchId, user.IsActive,
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
}
