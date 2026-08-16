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
public class ClinicsController(VetLimsDbContext db, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClinicDto>>> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var q = db.Clinics.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId.Value && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(x => EF.Functions.ILike(x.Name, s) || EF.Functions.ILike(x.Code, s));
        }

        var total = await q.CountAsync();
        var items = await q.OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ClinicDto(x.Id, x.OrganizationId!.Value, x.Code, x.Name, x.Logo, x.Address, x.Phone, x.Email, x.Gst, x.RegistrationNumber, x.WhatsAppNumber, x.ReportHeader, x.ReportFooter, x.DigitalSignature, x.IsActive))
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClinicDto>> Get(int id)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var item = await db.Clinics.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId.Value && !x.IsDeleted);
        if (item is null) return NotFound();

        return Ok(new ClinicDto(item.Id, item.OrganizationId!.Value, item.Code, item.Name, item.Logo, item.Address, item.Phone, item.Email, item.Gst, item.RegistrationNumber, item.WhatsAppNumber, item.ReportHeader, item.ReportFooter, item.DigitalSignature, item.IsActive));
    }

    [HttpPost]
    public async Task<ActionResult<ClinicDto>> Create([FromBody] ClinicCreateDto dto)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Clinic name is required.");
        if (string.IsNullOrWhiteSpace(dto.Code)) return BadRequest("Clinic code is required.");

        if (await db.Clinics.AnyAsync(x => x.OrganizationId == organizationId.Value && !x.IsDeleted && EF.Functions.ILike(x.Code, dto.Code.Trim())))
            return Conflict("Clinic code already exists in this organization.");

        var item = new Clinic
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

        db.Clinics.Add(item);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = item.Id }, new ClinicDto(item.Id, item.OrganizationId!.Value, item.Code, item.Name, item.Logo, item.Address, item.Phone, item.Email, item.Gst, item.RegistrationNumber, item.WhatsAppNumber, item.ReportHeader, item.ReportFooter, item.DigitalSignature, item.IsActive));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ClinicDto>> Update(int id, [FromBody] ClinicUpdateDto dto)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var item = await db.Clinics.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId.Value && !x.IsDeleted);
        if (item is null) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Clinic name is required.");
        if (string.IsNullOrWhiteSpace(dto.Code)) return BadRequest("Clinic code is required.");

        if (await db.Clinics.AnyAsync(x => x.Id != id && x.OrganizationId == organizationId.Value && !x.IsDeleted && EF.Functions.ILike(x.Code, dto.Code.Trim())))
            return Conflict("Clinic code already exists in this organization.");

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
        return Ok(new ClinicDto(item.Id, item.OrganizationId!.Value, item.Code, item.Name, item.Logo, item.Address, item.Phone, item.Email, item.Gst, item.RegistrationNumber, item.WhatsAppNumber, item.ReportHeader, item.ReportFooter, item.DigitalSignature, item.IsActive));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> ToggleStatus(int id, [FromBody] bool isActive)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var item = await db.Clinics.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId.Value && !x.IsDeleted);
        if (item is null) return NotFound();
        item.IsActive = isActive;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var item = await db.Clinics.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId.Value && !x.IsDeleted);
        if (item is null) return NotFound();

        db.Clinics.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }

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
}
