using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities; // where Species entity lives
using VetLims.WebApi.DTOs;

namespace VetLims.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // only Admin can manage master data
public class SpeciesController(VetLimsDbContext db) : ControllerBase
{
    // GET: api/species?search=cat&page=1&pageSize=10
    [HttpGet]
    public async Task<ActionResult<object>> Get([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var query = db.Species.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x => x.Code.ToLower().Contains(s) || x.Name.ToLower().Contains(s));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SpeciesDto(x.Id, x.Code, x.Name))
            .ToListAsync();

        return Ok(new { total, items, page, pageSize });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SpeciesDto>> GetOne(int id)
    {
        var e = await db.Species.FindAsync(id);
        if (e is null) return NotFound();
        return new SpeciesDto(e.Id, e.Code, e.Name);
    }

    [HttpPost]
    public async Task<ActionResult<SpeciesDto>> Create([FromBody] SpeciesCreateDto dto)
    {
        // unique code guard
        if (await db.Species.AnyAsync(x => x.Code == dto.Code))
            return Conflict("Code already exists.");

        var e = new Species { Code = dto.Code.Trim(), Name = dto.Name.Trim() };
        db.Species.Add(e);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetOne), new { id = e.Id }, new SpeciesDto(e.Id, e.Code, e.Name));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SpeciesDto>> Update(int id, [FromBody] SpeciesUpdateDto dto)
    {
        var e = await db.Species.FindAsync(id);
        if (e is null) return NotFound();

        // unique code guard (excluding self)
        if (await db.Species.AnyAsync(x => x.Code == dto.Code && x.Id != id))
            return Conflict("Code already exists.");

        e.Code = dto.Code.Trim();
        e.Name = dto.Name.Trim();
        await db.SaveChangesAsync();

        return new SpeciesDto(e.Id, e.Code, e.Name);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await db.Species.FindAsync(id);
        if (e is null) return NotFound();
        db.Species.Remove(e);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
