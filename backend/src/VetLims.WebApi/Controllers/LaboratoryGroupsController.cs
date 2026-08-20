using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;
using VetLims.WebApi.DTOs;

namespace VetLims.WebApi.Controllers;

[ApiController]
[Route("api/laboratory-groups")]
[Authorize(Roles = "Admin")]
public class LaboratoryGroupsController(VetLimsDbContext db, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LaboratoryGroupDto>>> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var q = db.LaboratoryGroups.AsNoTracking()
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
            .Select(x => new LaboratoryGroupDto(
                x.Id,
                x.OrganizationId!.Value,
                x.Code,
                x.Name,
                x.Description,
                x.IsActive,
                x.CreatedDate,
                x.ModifiedDate,
                x.IsDeleted,
                db.Laboratories.Count(c => c.OrganizationId == organizationId.Value && !c.IsDeleted && c.LaboratoryGroupId == x.Id)))
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LaboratoryGroupDto>> Get(int id)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var item = await db.LaboratoryGroups.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId.Value && !x.IsDeleted);

        if (item is null)
            return NotFound();

        var laboratoryCount = await db.Laboratories.CountAsync(x => x.OrganizationId == organizationId.Value && !x.IsDeleted && x.LaboratoryGroupId == id);

        return Ok(new LaboratoryGroupDto(
            item.Id,
            item.OrganizationId!.Value,
            item.Code,
            item.Name,
            item.Description,
            item.IsActive,
            item.CreatedDate,
            item.ModifiedDate,
            item.IsDeleted,
            laboratoryCount));
    }

    [HttpPost]
    public async Task<ActionResult<LaboratoryGroupDto>> Create([FromBody] LaboratoryGroupCreateDto dto)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Laboratory group name is required.");

        if (string.IsNullOrWhiteSpace(dto.Code))
            return BadRequest("Laboratory group code is required.");

        var code = dto.Code.Trim();
        var name = dto.Name.Trim();
        var description = dto.Description?.Trim();

        if (code.Length > 100)
            return BadRequest("Laboratory group code must be 100 characters or fewer.");

        if (name.Length > 200)
            return BadRequest("Laboratory group name must be 200 characters or fewer.");

        if (!string.IsNullOrWhiteSpace(description) && description.Length > 2000)
            return BadRequest("Laboratory group description must be 2000 characters or fewer.");

        if (await db.LaboratoryGroups.AnyAsync(x => x.OrganizationId == organizationId.Value && !x.IsDeleted && EF.Functions.ILike(x.Code, code)))
            return Conflict("Laboratory group code already exists in this organization.");

        var item = new LaboratoryGroup
        {
            OrganizationId = organizationId.Value,
            Code = code,
            Name = name,
            Description = description,
            IsActive = dto.IsActive,
            IsDeleted = false
        };

        db.LaboratoryGroups.Add(item);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = item.Id }, new LaboratoryGroupDto(
            item.Id,
            item.OrganizationId!.Value,
            item.Code,
            item.Name,
            item.Description,
            item.IsActive,
            item.CreatedDate,
            item.ModifiedDate,
            item.IsDeleted,
            0));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<LaboratoryGroupDto>> Update(int id, [FromBody] LaboratoryGroupUpdateDto dto)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var item = await db.LaboratoryGroups.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId.Value && !x.IsDeleted);
        if (item is null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Laboratory group name is required.");

        if (string.IsNullOrWhiteSpace(dto.Code))
            return BadRequest("Laboratory group code is required.");

        var code = dto.Code.Trim();
        var name = dto.Name.Trim();
        var description = dto.Description?.Trim();

        if (code.Length > 100)
            return BadRequest("Laboratory group code must be 100 characters or fewer.");

        if (name.Length > 200)
            return BadRequest("Laboratory group name must be 200 characters or fewer.");

        if (!string.IsNullOrWhiteSpace(description) && description.Length > 2000)
            return BadRequest("Laboratory group description must be 2000 characters or fewer.");

        if (await db.LaboratoryGroups.AnyAsync(x => x.Id != id && x.OrganizationId == organizationId.Value && !x.IsDeleted && EF.Functions.ILike(x.Code, code)))
            return Conflict("Laboratory group code already exists in this organization.");

        item.Code = code;
        item.Name = name;
        item.Description = description;
        item.IsActive = dto.IsActive;

        await db.SaveChangesAsync();

        var laboratoryCount = await db.Laboratories.CountAsync(x => x.OrganizationId == organizationId.Value && !x.IsDeleted && x.LaboratoryGroupId == id);

        return Ok(new LaboratoryGroupDto(
            item.Id,
            item.OrganizationId!.Value,
            item.Code,
            item.Name,
            item.Description,
            item.IsActive,
            item.CreatedDate,
            item.ModifiedDate,
            item.IsDeleted,
            laboratoryCount));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] bool isActive)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var item = await db.LaboratoryGroups.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId.Value && !x.IsDeleted);
        if (item is null)
            return NotFound();

        item.IsActive = isActive;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{laboratoryGroupId:int}/laboratories/{laboratoryId:int}")]
    public async Task<ActionResult<LaboratoryGroupLaboratoryDto>> AssignLaboratory(int laboratoryGroupId, int laboratoryId)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var labGroup = await db.LaboratoryGroups.FirstOrDefaultAsync(x => x.Id == laboratoryGroupId && x.OrganizationId == organizationId.Value && !x.IsDeleted);
        if (labGroup is null)
            return NotFound();

        if (!labGroup.IsActive)
            return Conflict("Laboratory group is inactive and cannot accept new laboratory assignments.");

        var laboratory = await db.Laboratories.FirstOrDefaultAsync(x => x.Id == laboratoryId && x.OrganizationId == organizationId.Value && !x.IsDeleted);
        if (laboratory is null)
            return NotFound();

        if (laboratory.OrganizationId != labGroup.OrganizationId)
            return Conflict("Laboratory and laboratory group must belong to the same organization.");

        laboratory.LaboratoryGroupId = labGroup.Id;
        await db.SaveChangesAsync();

        return Ok(new LaboratoryGroupLaboratoryDto(laboratory.Id, laboratory.OrganizationId!.Value, laboratory.Code, laboratory.Name, laboratory.ShortName, laboratory.IsActive, laboratory.LaboratoryGroupId));
    }

    [HttpDelete("{laboratoryGroupId:int}/laboratories/{laboratoryId:int}")]
    public async Task<IActionResult> UnassignLaboratory(int laboratoryGroupId, int laboratoryId)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var labGroup = await db.LaboratoryGroups.FirstOrDefaultAsync(x => x.Id == laboratoryGroupId && x.OrganizationId == organizationId.Value && !x.IsDeleted);
        if (labGroup is null)
            return NotFound();

        var laboratory = await db.Laboratories.FirstOrDefaultAsync(x => x.Id == laboratoryId && x.OrganizationId == organizationId.Value && !x.IsDeleted);
        if (laboratory is null)
            return NotFound();

        if (laboratory.LaboratoryGroupId != laboratoryGroupId)
            return Conflict("Laboratory is not assigned to the specified laboratory group.");

        laboratory.LaboratoryGroupId = null;
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id:int}/laboratories")]
    public async Task<ActionResult<IEnumerable<LaboratoryGroupLaboratoryDto>>> ListLaboratories(int id)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var labGroup = await db.LaboratoryGroups.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId.Value && !x.IsDeleted);
        if (labGroup is null)
            return NotFound();

        var items = await db.Laboratories.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId.Value && x.LaboratoryGroupId == id && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new LaboratoryGroupLaboratoryDto(
                x.Id,
                x.OrganizationId!.Value,
                x.Code,
                x.Name,
                x.ShortName,
                x.IsActive,
                x.LaboratoryGroupId))
            .ToListAsync();

        return Ok(items);
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
