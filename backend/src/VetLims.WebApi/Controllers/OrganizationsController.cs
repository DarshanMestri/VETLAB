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
public class OrganizationsController(VetLimsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrganizationDto>>> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        // Exclude soft-deleted records from normal listing
        var q = db.Organizations.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(x => (EF.Functions.ILike(x.Name, s) || EF.Functions.ILike(x.Code, s)) && !x.IsDeleted);
        }

        var total = await q.CountAsync();
        var items = await q.OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new OrganizationDto(x.Id, x.Code, x.Name, x.LegalName, x.Email, x.Phone, x.Address, x.IsActive))
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrganizationDto>> Get(int id)
    {
        // Treat soft-deleted organization as not found for normal API access
        var item = await db.Organizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (item is null) return NotFound();

        return Ok(new OrganizationDto(item.Id, item.Code, item.Name, item.LegalName, item.Email, item.Phone, item.Address, item.IsActive));
    }

    [HttpPost]
    public async Task<ActionResult<OrganizationDto>> Create([FromBody] OrganizationCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Organization name is required.");
        if (string.IsNullOrWhiteSpace(dto.Code)) return BadRequest("Organization code is required.");

        var normalizedCode = dto.Code.Trim();
        // Ignore soft-deleted organizations when checking for code uniqueness so codes can be reused
        if (await db.Organizations.AnyAsync(x => !x.IsDeleted && EF.Functions.ILike(x.Code, normalizedCode)))
            return Conflict("Organization code already exists.");

        var item = new Organization
        {
            Code = normalizedCode,
            Name = dto.Name.Trim(),
            LegalName = dto.LegalName?.Trim(),
            Email = dto.Email?.Trim(),
            Phone = dto.Phone?.Trim(),
            Address = dto.Address?.Trim(),
            IsActive = dto.IsActive
        };

        db.Organizations.Add(item);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = item.Id }, new OrganizationDto(item.Id, item.Code, item.Name, item.LegalName, item.Email, item.Phone, item.Address, item.IsActive));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<OrganizationDto>> Update(int id, [FromBody] OrganizationUpdateDto dto)
    {
        // Only allow updates on non-deleted organizations
        var item = await db.Organizations.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (item is null) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Organization name is required.");
        if (string.IsNullOrWhiteSpace(dto.Code)) return BadRequest("Organization code is required.");

        var normalizedCode = dto.Code.Trim();
        // Ignore soft-deleted organizations when checking duplicates
        if (await db.Organizations.AnyAsync(x => x.Id != id && !x.IsDeleted && EF.Functions.ILike(x.Code, normalizedCode)))
            return Conflict("Organization code already exists.");

        item.Code = normalizedCode;
        item.Name = dto.Name.Trim();
        item.LegalName = dto.LegalName?.Trim();
        item.Email = dto.Email?.Trim();
        item.Phone = dto.Phone?.Trim();
        item.Address = dto.Address?.Trim();
        item.IsActive = dto.IsActive;

        await db.SaveChangesAsync();
        return Ok(new OrganizationDto(item.Id, item.Code, item.Name, item.LegalName, item.Email, item.Phone, item.Address, item.IsActive));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] bool isActive)
    {
        var item = await db.Organizations.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (item is null) return NotFound();
        item.IsActive = isActive;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
