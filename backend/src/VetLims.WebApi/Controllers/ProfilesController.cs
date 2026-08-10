using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;
using VetLims.WebApi.Common;
using VetLims.WebApi.DTOs;

namespace VetLims.WebApi.Controllers;

[Authorize(Policy = "RequireManager")]
[ApiController]
[Route("api/[controller]")]
public class ProfilesController : ControllerBase
{
    private readonly VetLimsDbContext _db;
    public ProfilesController(VetLimsDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProfileDto>>> List([FromQuery] ListQuery q)
    {
        var (page, size) = PagingHelpers.Normalize(q.Page, q.PageSize);
        var query = _db.Profiles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = $"%{q.Search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Code, s) || EF.Functions.ILike(x.Name, s));
        }
        var total = await query.CountAsync();
        var items = await query.OrderBy(x => x.Id)
            .Skip((page - 1) * size).Take(size)
            .Select(x => new ProfileDto(x.Id, x.Code, x.Name, x.IsActive))
            .ToListAsync();

        return Ok(new PagedResult<ProfileDto> { Page = page, PageSize = size, Total = total, Items = items });
    }

    [HttpPost]
    public async Task<ActionResult<ProfileDto>> Create(ProfileCreateDto dto)
    {
        var e = new Profile { Code = dto.Code.Trim(), Name = dto.Name.Trim(), IsActive = dto.IsActive };
        _db.Profiles.Add(e); await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = e.Id }, new ProfileDto(e.Id, e.Code, e.Name, e.IsActive));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProfileDto>> Get(int id)
    {
        var e = await _db.Profiles.FindAsync(id);
        return e is null ? NotFound() : Ok(new ProfileDto(e.Id, e.Code, e.Name, e.IsActive));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, ProfileUpdateDto dto)
    {
        var e = await _db.Profiles.FindAsync(id);
        if (e is null) return NotFound();
        e.Code = dto.Code.Trim(); e.Name = dto.Name.Trim(); e.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var e = await _db.Profiles.FindAsync(id);
        if (e is null) return NotFound();
        _db.Profiles.Remove(e); await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---- ProfileTests ----
    [HttpGet("{profileId:int}/tests")]
    public async Task<ActionResult<IEnumerable<ProfileTestDto>>> ListTests(int profileId)
    {
        var items = await _db.ProfileTests.AsNoTracking()
            .Where(x => x.ProfileId == profileId)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new ProfileTestDto(x.ProfileId, x.TestId, x.DisplayOrder))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost("{profileId:int}/tests")]
    public async Task<ActionResult> AddTest(int profileId, ProfileTestCreateDto dto)
    {
        if (dto.ProfileId != profileId) return BadRequest("Mismatched ProfileId.");

        // Prevent duplicates
        var exists = await _db.ProfileTests.AnyAsync(x => x.ProfileId == dto.ProfileId && x.TestId == dto.TestId);
        if (exists) return Conflict("Test already added to profile.");

        var e = new ProfileTest { ProfileId = dto.ProfileId, TestId = dto.TestId, DisplayOrder = dto.DisplayOrder };
        _db.ProfileTests.Add(e); await _db.SaveChangesAsync();
        return StatusCode(201);
    }

    [HttpPut("{profileId:int}/tests/{testId:int}")]
    public async Task<ActionResult> UpdateTest(int profileId, int testId, ProfileTestUpdateDto dto)
    {
        var e = await _db.ProfileTests.FindAsync(profileId, testId);
        if (e is null) return NotFound();
        e.DisplayOrder = dto.DisplayOrder; await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{profileId:int}/tests/{testId:int}")]
    public async Task<ActionResult> RemoveTest(int profileId, int testId)
    {
        var e = await _db.ProfileTests.FindAsync(profileId, testId);
        if (e is null) return NotFound();
        _db.ProfileTests.Remove(e); await _db.SaveChangesAsync();
        return NoContent();
    }
}
