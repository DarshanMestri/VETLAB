using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;
using VetLims.WebApi.DTOs;

namespace VetLims.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class DepartmentsController(VetLimsDbContext db, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var q = db.Departments.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(x => EF.Functions.ILike(x.Name, s) || (x.Code != null && EF.Functions.ILike(x.Code, s)));
        }
        var total = await q.CountAsync();
        var items = await q.OrderBy(x => x.Name).Skip((page-1)*pageSize).Take(pageSize)
            .Select(x => new DepartmentDto(x.Id, x.Name, x.Code, x.IsActive)).ToListAsync();
        return Ok(new { total, items, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<DepartmentDto>> Create(DepartmentCreateDto dto)
    {
        var name = dto.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Department name is required.");

        var code = string.IsNullOrWhiteSpace(dto.Code) ? null : dto.Code.Trim();

        if (await db.Departments.AnyAsync(x => !x.IsDeleted && EF.Functions.ILike(x.Name, name)))
            return Conflict("Duplicate department not allowed.");
        if (code is not null && await db.Departments.AnyAsync(x => !x.IsDeleted && x.Code != null && EF.Functions.ILike(x.Code, code)))
            return Conflict("Duplicate department not allowed.");

        var e = new Department { Name = name, Code = code, IsActive = dto.IsActive };
        db.Departments.Add(e);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            var inner = ex.InnerException?.Message ?? string.Empty;
            if (inner.Contains("IX_departments_Code", StringComparison.OrdinalIgnoreCase) ||
                inner.Contains("IX_departments_Name", StringComparison.OrdinalIgnoreCase) ||
                inner.Contains("duplicate key value violates unique constraint", StringComparison.OrdinalIgnoreCase) ||
                inner.Contains("duplicate key value", StringComparison.OrdinalIgnoreCase) ||
                inner.Contains("unique constraint", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict("Duplicate department not allowed.");
            }

            return Problem(detail: "Unable to save department.", statusCode: 500);
        }

        return CreatedAtAction(nameof(Get), new { id = e.Id }, new DepartmentDto(e.Id, e.Name, e.Code, e.IsActive));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DepartmentDto>> Get(int id)
    {
        var e = await db.Departments.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (e is null) return NotFound();
        return new DepartmentDto(e.Id, e.Name, e.Code, e.IsActive);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DepartmentDto>> Update(int id, DepartmentUpdateDto dto)
    {
        var e = await db.Departments.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (e is null) return NotFound();

        var name = dto.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Department name is required.");

        var code = string.IsNullOrWhiteSpace(dto.Code) ? null : dto.Code.Trim();

        if (await db.Departments.AnyAsync(x => x.Id != id && !x.IsDeleted && EF.Functions.ILike(x.Name, name)))
            return Conflict("Duplicate department not allowed.");
        if (code is not null && await db.Departments.AnyAsync(x => x.Id != id && !x.IsDeleted && x.Code != null && EF.Functions.ILike(x.Code, code)))
            return Conflict("Duplicate department not allowed.");

        e.Name = name;
        e.Code = code;
        e.IsActive = dto.IsActive;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            var inner = ex.InnerException?.Message ?? string.Empty;
            if (inner.Contains("IX_departments_Code", StringComparison.OrdinalIgnoreCase) ||
                inner.Contains("IX_departments_Name", StringComparison.OrdinalIgnoreCase) ||
                inner.Contains("duplicate key value violates unique constraint", StringComparison.OrdinalIgnoreCase) ||
                inner.Contains("duplicate key value", StringComparison.OrdinalIgnoreCase) ||
                inner.Contains("unique constraint", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict("Duplicate department not allowed.");
            }

            return Problem(detail: "Unable to update department.", statusCode: 500);
        }

        return new DepartmentDto(e.Id, e.Name, e.Code, e.IsActive);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] bool isActive)
    {
        var e = await db.Departments.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (e is null) return NotFound();
        e.IsActive = isActive;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await db.Departments.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (e is null) return NotFound();
        e.IsDeleted = true;
        e.DeletedDate = DateTime.UtcNow;
        e.DeletedBy = currentUserService.UserName ?? "system";
        e.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
