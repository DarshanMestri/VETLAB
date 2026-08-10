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
public class ClientRateController(VetLimsDbContext db) : ControllerBase
{
    // GET api/clientrate?clientId=1&itemType=1
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int clientId, [FromQuery] RegItemType? itemType)
    {
        if (clientId <= 0) return BadRequest("clientId required.");

        var q = db.ClientRates.AsNoTracking().Where(r => r.ClientId == clientId);
        if (itemType.HasValue) q = q.Where(r => r.ItemType == itemType);

        var items = await q
            .OrderBy(r => r.ItemType).ThenBy(r => r.ItemId)
            .Select(r => new ClientRateDto(r.Id, r.ClientId, r.ItemId, r.ItemType, r.Rate, r.Currency, r.IsActive))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<ClientRateDto>> Create([FromBody] ClientRateCreateDto dto)
    {
        // avoid duplicates
        var exists = await db.ClientRates.AnyAsync(r => r.ClientId == dto.ClientId && r.ItemType == dto.ItemType && r.ItemId == dto.ItemId);
        if (exists) return Conflict("Rate already exists for this client and item.");

        var e = new ClientRate
        {
            ClientId = dto.ClientId,
            ItemId = dto.ItemId,
            ItemType = dto.ItemType,
            Rate = dto.Rate,
            Currency = dto.Currency,
            IsActive = dto.IsActive
        };
        db.ClientRates.Add(e);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = e.Id },
            new ClientRateDto(e.Id, e.ClientId, e.ItemId, e.ItemType, e.Rate, e.Currency, e.IsActive));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClientRateDto>> Get(int id)
    {
        var e = await db.ClientRates.FindAsync(id);
        if (e is null) return NotFound();
        return new ClientRateDto(e.Id, e.ClientId, e.ItemId, e.ItemType, e.Rate, e.Currency, e.IsActive);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ClientRateDto>> Update(int id, [FromBody] ClientRateUpdateDto dto)
    {
        var e = await db.ClientRates.FindAsync(id);
        if (e is null) return NotFound();

        var dup = await db.ClientRates.AnyAsync(r =>
            r.Id != id && r.ClientId == e.ClientId && r.ItemType == dto.ItemType && r.ItemId == dto.ItemId);
        if (dup) return Conflict("Rate already exists for this client and item.");

        e.ItemId = dto.ItemId;
        e.ItemType = dto.ItemType;
        e.Rate = dto.Rate;
        e.Currency = dto.Currency;
        e.IsActive = dto.IsActive;
        e.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return new ClientRateDto(e.Id, e.ClientId, e.ItemId, e.ItemType, e.Rate, e.Currency, e.IsActive);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await db.ClientRates.FindAsync(id);
        if (e is null) return NotFound();
        db.ClientRates.Remove(e);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
