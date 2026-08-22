using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;
using VetLims.WebApi.DTOs;

namespace VetLims.WebApi.Controllers;

[ApiController]
[Route("api/laboratories")]
[Authorize(Roles = "Admin")]
public class LaboratoriesController(VetLimsDbContext db, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LaboratoryDto>>> List(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? organizationId = null)
    {
        // Optional explicit scope (used by cross-org pickers such as the Branch
        // master): validated active org overrides the caller's own organization.
        if (organizationId.HasValue && !await db.Organizations.AsNoTracking()
                .AnyAsync(x => x.Id == organizationId.Value && !x.IsDeleted && x.IsActive))
            return BadRequest("Selected organization is invalid or inactive.");

        var scopeOrganizationId = organizationId ?? await ResolveCurrentOrganizationIdAsync();
        if (!scopeOrganizationId.HasValue)
            return Forbid();

        var q = db.Laboratories.AsNoTracking()
            .Where(x => x.OrganizationId == scopeOrganizationId.Value && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(x => EF.Functions.ILike(x.Name, s) || EF.Functions.ILike(x.Code, s));
        }

        var total = await q.CountAsync();
        var items = await q.OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new { x.Id, x.OrganizationId, x.Code, x.Name, x.Logo, x.Address, x.Phone, x.Email, x.Gst, x.RegistrationNumber, x.WhatsAppNumber, x.ReportHeader, x.ReportFooter, x.DigitalSignature, x.IsActive, OrganizationName = db.Organizations.Where(o => o.Id == x.OrganizationId).Select(o => (string?)o.Name).FirstOrDefault() })
            .ToListAsync();

        return Ok(new
        {
            total,
            page,
            pageSize,
            items = items.Select(x => new LaboratoryDto(x.Id, x.OrganizationId!.Value, x.OrganizationName, x.Code, x.Name, x.Logo, x.Address, x.Phone, x.Email, x.Gst, x.RegistrationNumber, x.WhatsAppNumber, x.ReportHeader, x.ReportFooter, x.DigitalSignature, x.IsActive))
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LaboratoryDto>> Get(int id)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var item = await db.Laboratories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId.Value && !x.IsDeleted);
        if (item is null) return NotFound();

        return Ok(ToDto(item));
    }

    [HttpPost]
    public async Task<ActionResult<LaboratoryDto>> Create([FromBody] LaboratoryCreateDto dto)
    {
        // The caller may place the new laboratory under any active organization
        // (defaults to their own home organization when omitted).
        var organizationId = await ResolveTargetOrganizationIdAsync(dto.OrganizationId);
        if (!organizationId.HasValue)
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Laboratory name is required.");
        if (string.IsNullOrWhiteSpace(dto.Code)) return BadRequest("Laboratory code is required.");

        if (await db.Laboratories.AnyAsync(x => x.OrganizationId == organizationId.Value && !x.IsDeleted && EF.Functions.ILike(x.Code, dto.Code.Trim())))
            return Conflict("Laboratory code already exists in this organization.");

        var item = new Laboratory
        {
            OrganizationId = organizationId.Value,
            Code = dto.Code.Trim(),
            Name = dto.Name.Trim(),
            Logo = dto.Logo?.Trim(),
            Address = dto.Address?.Trim(),
            Phone = dto.Phone?.Trim(),
            Email = dto.Email?.Trim(),
            Gst = dto.Gst?.Trim(),
            RegistrationNumber = dto.RegistrationNumber?.Trim(),
            WhatsAppNumber = dto.WhatsAppNumber?.Trim(),
            ReportHeader = dto.ReportHeader?.Trim(),
            ReportFooter = dto.ReportFooter?.Trim(),
            DigitalSignature = dto.DigitalSignature?.Trim(),
            IsActive = dto.IsActive
        };

        db.Laboratories.Add(item);
        await db.SaveChangesAsSystemAsync(organizationId.Value);

        return CreatedAtAction(nameof(Get), new { id = item.Id }, ToDto(item));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<LaboratoryDto>> Update(int id, [FromBody] LaboratoryUpdateDto dto)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var item = await db.Laboratories.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId.Value && !x.IsDeleted);
        if (item is null) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Laboratory name is required.");
        if (string.IsNullOrWhiteSpace(dto.Code)) return BadRequest("Laboratory code is required.");

        if (await db.Laboratories.AnyAsync(x => x.Id != id && x.OrganizationId == organizationId.Value && !x.IsDeleted && EF.Functions.ILike(x.Code, dto.Code.Trim())))
            return Conflict("Laboratory code already exists in this organization.");

        item.Code = dto.Code.Trim();
        item.Name = dto.Name.Trim();
        item.Logo = dto.Logo?.Trim();
        item.Address = dto.Address?.Trim();
        item.Phone = dto.Phone?.Trim();
        item.Email = dto.Email?.Trim();
        item.Gst = dto.Gst?.Trim();
        item.RegistrationNumber = dto.RegistrationNumber?.Trim();
        item.WhatsAppNumber = dto.WhatsAppNumber?.Trim();
        item.ReportHeader = dto.ReportHeader?.Trim();
        item.ReportFooter = dto.ReportFooter?.Trim();
        item.DigitalSignature = dto.DigitalSignature?.Trim();
        item.IsActive = dto.IsActive;

        await db.SaveChangesAsync();
        return Ok(ToDto(item));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> ToggleStatus(int id, [FromBody] bool isActive)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var item = await db.Laboratories.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId.Value && !x.IsDeleted);
        if (item is null) return NotFound();
        item.IsActive = isActive;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private LaboratoryDto ToDto(Laboratory item) =>
        new(item.Id, item.OrganizationId!.Value,
            db.Organizations.AsNoTracking().Where(o => o.Id == item.OrganizationId!.Value).Select(o => (string?)o.Name).FirstOrDefault() ?? string.Empty,
            item.Code, item.Name, item.Logo, item.Address, item.Phone, item.Email, item.Gst, item.RegistrationNumber, item.WhatsAppNumber, item.ReportHeader, item.ReportFooter, item.DigitalSignature, item.IsActive);

    private Task<int?> ResolveCurrentOrganizationIdAsync()
    {
        var orgId = currentUserService.OrganizationId;
        if (!orgId.HasValue)
            return Task.FromResult<int?>(null);

        return db.Organizations.AsNoTracking()
            .Where(x => x.Id == orgId.Value && !x.IsDeleted && x.IsActive)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<int?> ResolveTargetOrganizationIdAsync(int? requestedOrganizationId)
    {
        if (!requestedOrganizationId.HasValue)
            return await ResolveCurrentOrganizationIdAsync();

        var exists = await db.Organizations.AsNoTracking()
            .AnyAsync(x => x.Id == requestedOrganizationId.Value && !x.IsDeleted && x.IsActive);
        return exists ? requestedOrganizationId : null;
    }
}
