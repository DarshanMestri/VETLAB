using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;     // ← RateItemType + ClientRate
using VetLims.WebApi.DTOs; 

namespace VetLims.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class RatesController : ControllerBase
{
    private readonly VetLimsDbContext _db;
    public RatesController(VetLimsDbContext db) => _db = db;

    // GET: api/rates?clientId=1&itemType=Test
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? clientId, [FromQuery] RegItemType? itemType)
    {
        var q = _db.ClientRates.AsNoTracking();

        if (clientId.HasValue) q = q.Where(r => r.ClientId == clientId.Value);
        if (itemType.HasValue) q = q.Where(r => r.ItemType == itemType.Value);

        var items = await q
            .OrderBy(r => r.ClientId).ThenBy(r => r.ItemType).ThenBy(r => r.ItemId)
            .Select(r => new RateDto(
                r.Id,
                r.ClientId,
                r.ItemType,     // ← RateItemType
                r.ItemId,
                r.Rate,
                r.Currency ?? "AED",
                r.IsActive
            ))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RateDto>> Get(int id)
    {
        var r = await _db.ClientRates.FindAsync(id);
        if (r is null) return NotFound();

        return new RateDto(r.Id, r.ClientId, r.ItemType, r.ItemId, r.Rate, r.Currency ?? "AED", r.IsActive);
    }

    [HttpPost]
    public async Task<ActionResult<RateDto>> Create([FromBody] RateCreateDto dto)
    {
        // unique (ClientId, ItemType, ItemId) guard when ClientId provided
        if (dto.ClientId.HasValue)
        {
            var dup = await _db.ClientRates.AnyAsync(x =>
                x.ClientId == dto.ClientId.Value &&
                x.ItemType == dto.ItemType &&
                x.ItemId == dto.ItemId);
            if (dup) return Conflict("Rate already exists for this client and item.");
        }

        var e = new ClientRate
        {
            ClientId = dto.ClientId ?? 0, // if null, use 0 for “default rate” (or require non-null)
            ItemType = dto.ItemType,
            ItemId   = dto.ItemId,
            Rate     = dto.Price,
            Currency = dto.Currency,
            IsActive = dto.IsActive
        };

        _db.ClientRates.Add(e);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = e.Id },
            new RateDto(e.Id, e.ClientId, e.ItemType, e.ItemId, e.Rate, e.Currency ?? "AED", e.IsActive));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RateDto>> Update(int id, [FromBody] RateUpdateDto dto)
    {
        var e = await _db.ClientRates.FindAsync(id);
        if (e is null) return NotFound();

        if (dto.ClientId.HasValue)
        {
            var dup = await _db.ClientRates.AnyAsync(x =>
                x.Id != id &&
                x.ClientId == dto.ClientId.Value &&
                x.ItemType == dto.ItemType &&
                x.ItemId == dto.ItemId);
            if (dup) return Conflict("Rate already exists for this client and item.");
        }

        e.ClientId = dto.ClientId ?? e.ClientId;
        e.ItemType = dto.ItemType;
        e.ItemId   = dto.ItemId;
        e.Rate     = dto.Price;
        e.Currency = dto.Currency;
        e.IsActive = dto.IsActive;
        e.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return new RateDto(e.Id, e.ClientId, e.ItemType, e.ItemId, e.Rate, e.Currency ?? "AED", e.IsActive);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await _db.ClientRates.FindAsync(id);
        if (e is null) return NotFound();
        _db.ClientRates.Remove(e);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
