using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;
using VetLims.WebApi.DTOs;

namespace VetLims.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireAdmin")]
public class RolesController(VetLimsDbContext db, RoleManager<IdentityRole<int>> roleManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleDto>>> List()
    {
        var roles = await roleManager.Roles
            .OrderBy(x => x.Name)
            .Select(x => new RoleDto(x.Id, x.Name ?? string.Empty))
            .ToListAsync();

        return Ok(roles);
    }

    [HttpGet("{roleName}/permissions")]
    public async Task<ActionResult<IEnumerable<RolePermissionDto>>> GetPermissions(string roleName)
    {
        var items = await db.RolePermissions
            .Where(x => x.RoleName == roleName)
            .OrderBy(x => x.PermissionKey)
            .Select(x => new RolePermissionDto(x.RoleName, x.PermissionKey))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create([FromBody] RoleCreateDto dto)
    {
        var roleName = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(roleName))
            return BadRequest("Role name is required.");

        if (await roleManager.RoleExistsAsync(roleName))
            return Conflict("Role already exists.");

        var role = new IdentityRole<int> { Name = roleName };
        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
            return BadRequest(string.Join("; ", result.Errors.Select(e => e.Description)));

        return CreatedAtAction(nameof(List), new RoleDto(role.Id, roleName));
    }

    [HttpPut("{roleName}")]
    public async Task<IActionResult> Update(string roleName, [FromBody] RoleUpdateDto dto)
    {
        var existing = await roleManager.FindByNameAsync(roleName);
        if (existing is null) return NotFound();

        var newName = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(newName))
            return BadRequest("Role name is required.");

        if (newName != roleName && await roleManager.RoleExistsAsync(newName))
            return Conflict("The target role name already exists.");

        existing.Name = newName;
        var result = await roleManager.UpdateAsync(existing);
        if (!result.Succeeded)
            return BadRequest(string.Join("; ", result.Errors.Select(e => e.Description)));

        await db.RolePermissions
            .Where(x => x.RoleName == roleName)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.RoleName, newName));

        return NoContent();
    }

    [HttpDelete("{roleName}")]
    public async Task<IActionResult> Delete(string roleName)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role is null) return NotFound();

        var result = await roleManager.DeleteAsync(role);
        if (!result.Succeeded)
            return BadRequest(string.Join("; ", result.Errors.Select(e => e.Description)));

        await db.RolePermissions.Where(x => x.RoleName == roleName).ExecuteDeleteAsync();
        return NoContent();
    }

    [HttpPost("{roleName}/permissions")]
    public async Task<ActionResult> AssignPermission(string roleName, [FromBody] RolePermissionAssignDto dto)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role is null) return NotFound();

        var permission = await db.Permissions.FirstOrDefaultAsync(x => x.Key == dto.PermissionKey);
        if (permission is null) return NotFound("Permission not found.");

        var exists = await db.RolePermissions.AnyAsync(x => x.RoleName == roleName && x.PermissionKey == permission.Key);
        if (exists) return Conflict("Permission already assigned to the role.");

        db.RolePermissions.Add(new RolePermission
        {
            RoleName = roleName,
            PermissionId = permission.Id,
            PermissionKey = permission.Key
        });

        await db.SaveChangesAsync();
        return Ok(new RolePermissionDto(roleName, permission.Key));
    }

    [HttpDelete("{roleName}/permissions/{permissionKey}")]
    public async Task<IActionResult> RemovePermission(string roleName, string permissionKey)
    {
        var item = await db.RolePermissions.FirstOrDefaultAsync(x => x.RoleName == roleName && x.PermissionKey == permissionKey);
        if (item is null) return NotFound();

        db.RolePermissions.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
