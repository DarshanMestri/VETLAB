using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;
using VetLims.WebApi.DTOs;

namespace VetLims.WebApi.Controllers;

// If you have a "RequireManager" policy configured, keep it;
// otherwise use Roles or plain [Authorize]
[Authorize(Policy = "RequireManager")]
[ApiController]
[Route("api/reference-ranges")]
public class ReferenceRangesController : ControllerBase
{
    private readonly VetLimsDbContext _db;
    public ReferenceRangesController(VetLimsDbContext db) => _db = db;

    // GET: api/reference-ranges/by-parameter/123
    [HttpGet("by-parameter/{parameterId:int}")]
    public async Task<ActionResult<IEnumerable<TestReferenceRangeDto>>> ByParameter(int parameterId)
    {
        var list = await _db.TestReferenceRanges.AsNoTracking()
            .Where(x => x.TestParameterId == parameterId)
            .OrderBy(x => x.AgeFromDays).ThenBy(x => x.AgeToDays)
            .Select(x => new TestReferenceRangeDto(
                x.Id,
                x.TestParameterId,
                x.SpeciesId,
                x.AgeFromDays,
                x.AgeToDays,
                x.Low,
                x.High,
                x.Note,
                x.IsActive
            ))
            .ToListAsync();

        return Ok(list);
    }

    // POST: api/reference-ranges
    [HttpPost]
    public async Task<ActionResult<TestReferenceRangeDto>> Create([FromBody] TestReferenceRangeCreateDto dto)
    {
        var e = new TestReferenceRange
        {
            TestParameterId = dto.TestParameterId,
            SpeciesId = dto.SpeciesId,
            AgeFromDays = dto.AgeFromDays,
            AgeToDays = dto.AgeToDays,
            Low = dto.Low,
            High = dto.High,
            Note = dto.Note,
            IsActive = dto.IsActive
        };

        _db.TestReferenceRanges.Add(e);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = e.Id }, new TestReferenceRangeDto(
            e.Id, e.TestParameterId, e.SpeciesId, e.AgeFromDays, e.AgeToDays, e.Low, e.High, e.Note, e.IsActive
        ));
    }

    // GET: api/reference-ranges/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TestReferenceRangeDto>> Get(int id)
    {
        var x = await _db.TestReferenceRanges.FindAsync(id);
        if (x is null) return NotFound();

        return new TestReferenceRangeDto(
            x.Id, x.TestParameterId, x.SpeciesId, x.AgeFromDays, x.AgeToDays, x.Low, x.High, x.Note, x.IsActive
        );
    }

    // PUT: api/reference-ranges/5
    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] TestReferenceRangeUpdateDto dto)
    {
        var x = await _db.TestReferenceRanges.FindAsync(id);
        if (x is null) return NotFound();

        // Optional duplicate guard: same param/species/age-band
        var dup = await _db.TestReferenceRanges.AnyAsync(r =>
            r.Id != id &&
            r.TestParameterId == x.TestParameterId &&
            r.SpeciesId == dto.SpeciesId &&
            r.AgeFromDays == dto.AgeFromDays &&
            r.AgeToDays == dto.AgeToDays
        );
        if (dup) return Conflict("Range for the same species and age band already exists.");

        x.SpeciesId   = dto.SpeciesId;
        x.AgeFromDays = dto.AgeFromDays;
        x.AgeToDays   = dto.AgeToDays;
        x.Low         = dto.Low;
        x.High        = dto.High;
        x.Note        = dto.Note;
        x.IsActive    = dto.IsActive;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/reference-ranges/5
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var x = await _db.TestReferenceRanges.FindAsync(id);
        if (x is null) return NotFound();

        _db.TestReferenceRanges.Remove(x);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
