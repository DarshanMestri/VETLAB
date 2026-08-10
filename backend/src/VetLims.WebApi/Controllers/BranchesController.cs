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
public class BranchesController(VetLimsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BranchDto>>> List([FromQuery] int? clinicId, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var q = db.Branches.AsNoTracking().Include(x => x.Clinic).AsQueryable();
        if (clinicId.HasValue) q = q.Where(x => x.ClinicId == clinicId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(x => EF.Functions.ILike(x.Name, s) || EF.Functions.ILike(x.Code, s));
        }

        var total = await q.CountAsync();
        var items = await q.OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BranchDto(x.Id, x.ClinicId, x.Code, x.Name, x.Address, x.Phone, x.Email, x.IsActive))
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BranchDto>> Get(int id)
    {
        var item = await db.Branches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();

        return Ok(new BranchDto(item.Id, item.ClinicId, item.Code, item.Name, item.Address, item.Phone, item.Email, item.IsActive));
    }

    [HttpPost]
    public async Task<ActionResult<BranchDto>> Create([FromBody] BranchCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Branch name is required.");
        if (string.IsNullOrWhiteSpace(dto.Code)) return BadRequest("Branch code is required.");
        if (!await db.Clinics.AnyAsync(x => x.Id == dto.ClinicId)) return NotFound("Clinic not found.");

        if (await db.Branches.AnyAsync(x => x.ClinicId == dto.ClinicId && EF.Functions.ILike(x.Code, dto.Code.Trim())))
            return Conflict("Branch code already exists in this clinic.");

        var item = new Branch
        {
            ClinicId = dto.ClinicId,
            Code = dto.Code.Trim(),
            Name = dto.Name.Trim(),
            Address = dto.Address?.Trim(),
            Phone = dto.Phone?.Trim(),
            Email = dto.Email?.Trim(),
            IsActive = dto.IsActive
        };

        db.Branches.Add(item);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = item.Id }, new BranchDto(item.Id, item.ClinicId, item.Code, item.Name, item.Address, item.Phone, item.Email, item.IsActive));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<BranchDto>> Update(int id, [FromBody] BranchUpdateDto dto)
    {
        var item = await db.Branches.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Branch name is required.");
        if (string.IsNullOrWhiteSpace(dto.Code)) return BadRequest("Branch code is required.");
        if (!await db.Clinics.AnyAsync(x => x.Id == dto.ClinicId)) return NotFound("Clinic not found.");

        if (await db.Branches.AnyAsync(x => x.Id != id && x.ClinicId == dto.ClinicId && EF.Functions.ILike(x.Code, dto.Code.Trim())))
            return Conflict("Branch code already exists in this clinic.");

        item.ClinicId = dto.ClinicId;
        item.Code = dto.Code.Trim();
        item.Name = dto.Name.Trim();
        item.Address = dto.Address?.Trim();
        item.Phone = dto.Phone?.Trim();
        item.Email = dto.Email?.Trim();
        item.IsActive = dto.IsActive;

        await db.SaveChangesAsync();
        return Ok(new BranchDto(item.Id, item.ClinicId, item.Code, item.Name, item.Address, item.Phone, item.Email, item.IsActive));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> ToggleStatus(int id, [FromBody] bool isActive)
    {
        var item = await db.Branches.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        item.IsActive = isActive;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Branches.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();

        db.Branches.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
