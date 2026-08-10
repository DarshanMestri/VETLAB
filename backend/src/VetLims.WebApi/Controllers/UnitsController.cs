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
public class UnitsController(VetLimsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var q = db.Units.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(x => EF.Functions.ILike(x.Name, s) || (x.Symbol != null && EF.Functions.ILike(x.Symbol, s)));
        }
        var total = await q.CountAsync();
        var items = await q.OrderBy(x => x.Name).Skip((page-1)*pageSize).Take(pageSize)
            .Select(x => new UnitDto(x.Id, x.Name, x.Symbol, x.IsActive)).ToListAsync();
        return Ok(new { total, items, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<UnitDto>> Create(UnitCreateDto dto)
    {
        var name = dto.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Unit name is required.");

        var symbol = dto.Symbol?.Trim();
        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest("Unit symbol is required.");

        if (await db.Units.AnyAsync(x => EF.Functions.ILike(x.Name, name)))
            return Conflict("Unit name already exists.");
        if (await db.Units.AnyAsync(x => x.Symbol != null && EF.Functions.ILike(x.Symbol, symbol)))
            return Conflict("Unit symbol already exists.");

        var e = new Unit { Name = name, Symbol = symbol, IsActive = dto.IsActive };
        db.Units.Add(e);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            var inner = ex.InnerException?.Message ?? string.Empty;
            if (inner.Contains("IX_units_Name", StringComparison.OrdinalIgnoreCase))
                return Conflict("Unit name already exists.");
            if (inner.Contains("IX_units_Symbol", StringComparison.OrdinalIgnoreCase))
                return Conflict("Unit symbol already exists.");
            if (inner.Contains("duplicate key value violates unique constraint", StringComparison.OrdinalIgnoreCase) ||
                inner.Contains("duplicate key value", StringComparison.OrdinalIgnoreCase) ||
                inner.Contains("unique constraint", StringComparison.OrdinalIgnoreCase))
                return Conflict("Duplicate unit not allowed.");

            return Problem(detail: "Unable to save unit.", statusCode: 500);
        }

        return CreatedAtAction(nameof(Get), new { id = e.Id }, new UnitDto(e.Id, e.Name, e.Symbol, e.IsActive));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UnitDto>> Get(int id)
    {
        var e = await db.Units.FindAsync(id);
        if (e is null) return NotFound();
        return new UnitDto(e.Id, e.Name, e.Symbol, e.IsActive);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UnitDto>> Update(int id, UnitUpdateDto dto)
    {
        var e = await db.Units.FindAsync(id);
        if (e is null) return NotFound();

        var name = dto.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Unit name is required.");

        var symbol = dto.Symbol?.Trim();
        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest("Unit symbol is required.");

        if (await db.Units.AnyAsync(x => x.Id != id && EF.Functions.ILike(x.Name, name)))
            return Conflict("Unit name already exists.");
        if (await db.Units.AnyAsync(x => x.Id != id && x.Symbol != null && EF.Functions.ILike(x.Symbol, symbol)))
            return Conflict("Unit symbol already exists.");

        e.Name = name;
        e.Symbol = symbol;
        e.IsActive = dto.IsActive;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            var inner = ex.InnerException?.Message ?? string.Empty;
            if (inner.Contains("IX_units_Name", StringComparison.OrdinalIgnoreCase))
                return Conflict("Unit name already exists.");
            if (inner.Contains("IX_units_Symbol", StringComparison.OrdinalIgnoreCase))
                return Conflict("Unit symbol already exists.");
            if (inner.Contains("duplicate key value violates unique constraint", StringComparison.OrdinalIgnoreCase) ||
                inner.Contains("duplicate key value", StringComparison.OrdinalIgnoreCase) ||
                inner.Contains("unique constraint", StringComparison.OrdinalIgnoreCase))
                return Conflict("Duplicate unit not allowed.");

            return Problem(detail: "Unable to update unit.", statusCode: 500);
        }

        return new UnitDto(e.Id, e.Name, e.Symbol, e.IsActive);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await db.Units.FindAsync(id);
        if (e is null) return NotFound();
        db.Units.Remove(e);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
