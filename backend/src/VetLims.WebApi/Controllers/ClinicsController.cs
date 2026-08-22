using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;
using VetLims.WebApi.DTOs;

namespace VetLims.WebApi.Controllers;

[ApiController]
[Route("api/clinics")]
[Authorize(Roles = "Admin")]
public class ClinicsController(VetLimsDbContext db, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClinicDto>>> List(
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

        var q = db.Clinics.AsNoTracking()
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
            .Select(x => new ClinicDto(x.Id, x.OrganizationId!.Value, x.Code, x.Name, x.ShortName, x.Phone, x.Email, x.Address, x.Logo, x.LicenseNumber, x.IsActive))
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

        return Ok(new ClinicDto(item.Id, item.OrganizationId!.Value, item.Code, item.Name, item.ShortName, item.Phone, item.Email, item.Address, item.Logo, item.LicenseNumber, item.IsActive));
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
            ShortName = dto.ShortName?.Trim(),
            Phone = dto.Phone?.Trim(),
            Email = dto.Email?.Trim(),
            Address = dto.Address?.Trim(),
            Logo = dto.Logo?.Trim(),
            LicenseNumber = dto.LicenseNumber?.Trim(),
            IsActive = dto.IsActive
        };

        db.Clinics.Add(item);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = item.Id }, new ClinicDto(item.Id, item.OrganizationId!.Value, item.Code, item.Name, item.ShortName, item.Phone, item.Email, item.Address, item.Logo, item.LicenseNumber, item.IsActive));
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
        item.ShortName = dto.ShortName?.Trim();
        item.Phone = dto.Phone?.Trim();
        item.Email = dto.Email?.Trim();
        item.Address = dto.Address?.Trim();
        item.Logo = dto.Logo?.Trim();
        item.LicenseNumber = dto.LicenseNumber?.Trim();
        item.IsActive = dto.IsActive;

        await db.SaveChangesAsync();
        return Ok(new ClinicDto(item.Id, item.OrganizationId!.Value, item.Code, item.Name, item.ShortName, item.Phone, item.Email, item.Address, item.Logo, item.LicenseNumber, item.IsActive));
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
