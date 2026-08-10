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
public class ClinicsController(VetLimsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClinicDto>>> List([FromQuery] int? organizationId, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var q = db.Clinics.AsNoTracking();
        if (organizationId.HasValue)
        {
            q = q.Where(x => x.OrganizationId == organizationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(x => EF.Functions.ILike(x.Name, s) || EF.Functions.ILike(x.Code, s));
        }

        var total = await q.CountAsync();
        var items = await q.OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ClinicDto(x.Id, x.OrganizationId, x.Code, x.Name, x.Logo, x.Address, x.Phone, x.Email, x.Gst, x.RegistrationNumber, x.WhatsAppNumber, x.ReportHeader, x.ReportFooter, x.DigitalSignature, x.IsActive))
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClinicDto>> Get(int id)
    {
        var item = await db.Clinics.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();

        return Ok(new ClinicDto(item.Id, item.OrganizationId, item.Code, item.Name, item.Logo, item.Address, item.Phone, item.Email, item.Gst, item.RegistrationNumber, item.WhatsAppNumber, item.ReportHeader, item.ReportFooter, item.DigitalSignature, item.IsActive));
    }

    [HttpPost]
    public async Task<ActionResult<ClinicDto>> Create([FromBody] ClinicCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Clinic name is required.");
        if (string.IsNullOrWhiteSpace(dto.Code)) return BadRequest("Clinic code is required.");
        var organizationId = await ResolveOrganizationIdAsync(dto.OrganizationId);
        if (organizationId is null) return BadRequest("Organization is required.");

        if (await db.Clinics.AnyAsync(x => x.OrganizationId == organizationId.Value && EF.Functions.ILike(x.Code, dto.Code.Trim())))
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

        return CreatedAtAction(nameof(Get), new { id = item.Id }, new ClinicDto(item.Id, item.OrganizationId, item.Code, item.Name, item.Logo, item.Address, item.Phone, item.Email, item.Gst, item.RegistrationNumber, item.WhatsAppNumber, item.ReportHeader, item.ReportFooter, item.DigitalSignature, item.IsActive));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ClinicDto>> Update(int id, [FromBody] ClinicUpdateDto dto)
    {
        var item = await db.Clinics.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Clinic name is required.");
        if (string.IsNullOrWhiteSpace(dto.Code)) return BadRequest("Clinic code is required.");
        var organizationId = await ResolveOrganizationIdAsync(dto.OrganizationId);
        if (organizationId is null) return BadRequest("Organization is required.");

        if (await db.Clinics.AnyAsync(x => x.Id != id && x.OrganizationId == organizationId.Value && EF.Functions.ILike(x.Code, dto.Code.Trim())))
            return Conflict("Clinic code already exists in this organization.");

        item.OrganizationId = organizationId.Value;
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
        return Ok(new ClinicDto(item.Id, item.OrganizationId, item.Code, item.Name, item.Logo, item.Address, item.Phone, item.Email, item.Gst, item.RegistrationNumber, item.WhatsAppNumber, item.ReportHeader, item.ReportFooter, item.DigitalSignature, item.IsActive));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> ToggleStatus(int id, [FromBody] bool isActive)
    {
        var item = await db.Clinics.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        item.IsActive = isActive;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Clinics.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();

        db.Clinics.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<int?> ResolveOrganizationIdAsync(int? requestedOrganizationId)
    {
        if (requestedOrganizationId.HasValue)
        {
            var exists = await db.Organizations.AnyAsync(x => x.Id == requestedOrganizationId.Value && x.IsActive);
            return exists ? requestedOrganizationId.Value : null;
        }

        return await db.Organizations
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();
    }
}
