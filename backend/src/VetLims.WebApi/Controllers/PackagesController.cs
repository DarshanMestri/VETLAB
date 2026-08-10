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
public class PackagesController : ControllerBase
{
    private readonly VetLimsDbContext _db;
    public PackagesController(VetLimsDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PagedResult<PackageDto>>> List([FromQuery] ListQuery q)
    {
        var (page, size) = PagingHelpers.Normalize(q.Page, q.PageSize);
        var query = _db.Packages.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = $"%{q.Search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Code, s) || EF.Functions.ILike(x.Name, s));
        }
        var total = await query.CountAsync();
        var items = await query.OrderBy(x => x.Id)
            .Skip((page - 1) * size).Take(size)
            .Select(x => new PackageDto(x.Id, x.Code, x.Name, x.IsActive))
            .ToListAsync();

        return Ok(new PagedResult<PackageDto> { Page = page, PageSize = size, Total = total, Items = items });
    }

    [HttpPost]
    public async Task<ActionResult<PackageDto>> Create(PackageCreateDto dto)
    {
        var e = new Package { Code = dto.Code.Trim(), Name = dto.Name.Trim(), IsActive = dto.IsActive };
        _db.Packages.Add(e); await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = e.Id }, new PackageDto(e.Id, e.Code, e.Name, e.IsActive));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PackageDto>> Get(int id)
    {
        var e = await _db.Packages.FindAsync(id);
        return e is null ? NotFound() : Ok(new PackageDto(e.Id, e.Code, e.Name, e.IsActive));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, PackageUpdateDto dto)
    {
        var e = await _db.Packages.FindAsync(id);
        if (e is null) return NotFound();
        e.Code = dto.Code.Trim(); e.Name = dto.Name.Trim(); e.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var e = await _db.Packages.FindAsync(id);
        if (e is null) return NotFound();
        _db.Packages.Remove(e); await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---- Package Items ----
    [HttpGet("{packageId:int}/items")]
    public async Task<ActionResult<IEnumerable<PackageItemDto>>> ListItems(int packageId)
    {
        var items = await _db.PackageItems.AsNoTracking()
            .Where(x => x.PackageId == packageId)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new PackageItemDto(
                x.Id,
                x.PackageId,
                (PackageItemTypeDto)x.ItemType, // map enum
                x.ItemId,
                x.DisplayOrder))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("{packageId:int}/items")]
    public async Task<ActionResult> AddItem(int packageId, PackageItemCreateDto dto)
    {
        if (dto.PackageId != packageId) return BadRequest("Mismatched PackageId.");

        // prevent duplicates
        var exists = await _db.PackageItems.AnyAsync(x =>
            x.PackageId == dto.PackageId &&
            x.ItemType == (PackageItemType)dto.ItemType &&
            x.ItemId == dto.ItemId);
        if (exists) return Conflict("Item already in package.");

        var e = new PackageItem
        {
            PackageId = dto.PackageId,
            ItemType = (PackageItemType)dto.ItemType,
            ItemId = dto.ItemId,
            DisplayOrder = dto.DisplayOrder
        };
        _db.PackageItems.Add(e); await _db.SaveChangesAsync();
        return StatusCode(201);
    }

    [HttpPut("{packageId:int}/items/{itemId:int}")]
    public async Task<ActionResult> UpdateItem(int packageId, int itemId, PackageItemUpdateDto dto)
    {
        var e = await _db.PackageItems.FindAsync(itemId);
        if (e is null || e.PackageId != packageId) return NotFound();

        // Optional: ensure no duplicate after update
        var dup = await _db.PackageItems.AnyAsync(x =>
            x.Id != itemId &&
            x.PackageId == packageId &&
            x.ItemType == (PackageItemType)dto.ItemType &&
            x.ItemId == dto.ItemId);
        if (dup) return Conflict("Another item with the same type and id already exists in this package.");

        e.ItemType = (PackageItemType)dto.ItemType;
        e.ItemId = dto.ItemId;
        e.DisplayOrder = dto.DisplayOrder;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{packageId:int}/items/{itemId:int}")]
    public async Task<ActionResult> RemoveItem(int packageId, int itemId)
    {
        var e = await _db.PackageItems.FindAsync(itemId);
        if (e is null || e.PackageId != packageId) return NotFound();
        _db.PackageItems.Remove(e); await _db.SaveChangesAsync();
        return NoContent();
    }
}
