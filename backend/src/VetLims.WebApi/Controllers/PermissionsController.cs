using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;
using VetLims.WebApi.DTOs;

namespace VetLims.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireAdmin")]
public class PermissionsController(VetLimsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PermissionDto>>> List()
    {
        var items = await db.Permissions
            .OrderBy(x => x.GroupName)
            .ThenBy(x => x.Name)
            .Select(x => new PermissionDto(x.Id, x.Name, x.Key, x.GroupName, x.Description, x.IsActive))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<PermissionDto>> Create([FromBody] PermissionCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Permission name is required.");
        if (string.IsNullOrWhiteSpace(dto.Key)) return BadRequest("Permission key is required.");

        if (await db.Permissions.AnyAsync(x => x.Key == dto.Key.Trim()))
            return Conflict("Permission key already exists.");

        var entity = new Permission
        {
            Name = dto.Name.Trim(),
            Key = dto.Key.Trim(),
            GroupName = dto.GroupName?.Trim(),
            Description = dto.Description?.Trim(),
            IsActive = dto.IsActive
        };

        db.Permissions.Add(entity);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, new PermissionDto(entity.Id, entity.Name, entity.Key, entity.GroupName, entity.Description, entity.IsActive));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PermissionDto>> Get(int id)
    {
        var item = await db.Permissions.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();

        return Ok(new PermissionDto(item.Id, item.Name, item.Key, item.GroupName, item.Description, item.IsActive));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PermissionDto>> Update(int id, [FromBody] PermissionUpdateDto dto)
    {
        var item = await db.Permissions.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();

        item.Name = dto.Name.Trim();
        item.Key = dto.Key.Trim();
        item.GroupName = dto.GroupName?.Trim();
        item.Description = dto.Description?.Trim();
        item.IsActive = dto.IsActive;

        await db.SaveChangesAsync();
        return Ok(new PermissionDto(item.Id, item.Name, item.Key, item.GroupName, item.Description, item.IsActive));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Permissions.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();

        db.Permissions.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("menu")]
    public async Task<ActionResult<IEnumerable<MenuPermissionDto>>> ListMenuPermissions()
    {
        var items = await db.MenuPermissions
            .OrderBy(x => x.Name)
            .Select(x => new MenuPermissionDto(x.Id, x.Code, x.Name, x.Description, x.IsActive))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("screens")]
    public async Task<ActionResult<IEnumerable<ScreenPermissionDto>>> ListScreenPermissions()
    {
        var items = await db.ScreenPermissions
            .OrderBy(x => x.Name)
            .Select(x => new ScreenPermissionDto(x.Id, x.Code, x.Name, x.MenuPermissionId, x.Description, x.IsActive))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("buttons")]
    public async Task<ActionResult<IEnumerable<ButtonPermissionDto>>> ListButtonPermissions()
    {
        var items = await db.ButtonPermissions
            .OrderBy(x => x.Name)
            .Select(x => new ButtonPermissionDto(x.Id, x.Code, x.Name, x.ScreenPermissionId, x.Description, x.IsActive))
            .ToListAsync();

        return Ok(items);
    }
}
