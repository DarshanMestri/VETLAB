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
public class TestsController(VetLimsDbContext db) : ControllerBase
{
    // --- Tests ---
    [HttpGet]
    public async Task<IActionResult> ListTests([FromQuery] string? search, [FromQuery] int? departmentId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var q = db.Tests.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(x => EF.Functions.ILike(x.Name, s)
                || EF.Functions.ILike(x.DisplayName ?? string.Empty, s)
                || (x.Code != null && EF.Functions.ILike(x.Code, s)));
        }
        if (departmentId.HasValue) q = q.Where(x => x.DepartmentId == departmentId.Value);

        var total = await q.CountAsync();
        var items = await q.OrderBy(x => x.Name).Skip((page-1)*pageSize).Take(pageSize)
            .Select(x => new TestDto(x.Id, x.Name, x.DisplayName, x.Code, x.DepartmentId, x.UnitId, x.ValueType, x.IsActive))
            .ToListAsync();

        return Ok(new { total, items, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<TestDto>> CreateTest([FromBody] TestCreateDto dto)
    {
        if (dto.DepartmentId <= 0)
            return BadRequest("Department is required.");

        if (!await db.Departments.AnyAsync(x => x.Id == dto.DepartmentId))
            return BadRequest("Selected department does not exist.");

        if (dto.UnitId.HasValue && !await db.Units.AnyAsync(x => x.Id == dto.UnitId.Value))
            return BadRequest("Selected unit does not exist.");

        var code = string.IsNullOrWhiteSpace(dto.Code) ? null : dto.Code.Trim();
        if (code is not null && await db.Tests.AnyAsync(x => x.Code != null && EF.Functions.ILike(x.Code, code)))
            return Conflict("Test code must be unique.");

        var e = new Test
        {
            Name = dto.Name.Trim(),
            DisplayName = dto.DisplayName?.Trim(),
            Code = code,
            DepartmentId = dto.DepartmentId,
            UnitId = dto.UnitId,
            ValueType = dto.ValueType,
            IsActive = dto.IsActive
        };
        db.Tests.Add(e);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            var inner = ex.InnerException?.Message ?? string.Empty;
            if (inner.Contains("FK_tests_departments_DepartmentId", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Selected department does not exist.");
            if (inner.Contains("FK_tests_units_UnitId", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Selected unit does not exist.");
            if (inner.Contains("IX_tests_Code", StringComparison.OrdinalIgnoreCase)
                || inner.Contains("tests_code", StringComparison.OrdinalIgnoreCase)
                || (inner.Contains("unique", StringComparison.OrdinalIgnoreCase) && inner.Contains("Code", StringComparison.OrdinalIgnoreCase)))
                return Conflict("Test code must be unique.");
            return Problem(detail: "Unable to save test.", statusCode: 500);
        }

        return CreatedAtAction(nameof(GetTest), new { id = e.Id }, new TestDto(e.Id, e.Name, e.DisplayName, e.Code, e.DepartmentId, e.UnitId, e.ValueType, e.IsActive));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TestDto>> GetTest(int id)
    {
        var e = await db.Tests.FindAsync(id);
        if (e is null) return NotFound();
        return new TestDto(e.Id, e.Name, e.DisplayName, e.Code, e.DepartmentId, e.UnitId, e.ValueType, e.IsActive);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TestDto>> UpdateTest(int id, [FromBody] TestUpdateDto dto)
    {
        var e = await db.Tests.FindAsync(id);
        if (e is null) return NotFound();

        if (dto.DepartmentId <= 0)
            return BadRequest("Department is required.");

        if (!await db.Departments.AnyAsync(x => x.Id == dto.DepartmentId))
            return BadRequest("Selected department does not exist.");

        if (dto.UnitId.HasValue && !await db.Units.AnyAsync(x => x.Id == dto.UnitId.Value))
            return BadRequest("Selected unit does not exist.");

        var code = string.IsNullOrWhiteSpace(dto.Code) ? null : dto.Code.Trim();
        if (code is not null && await db.Tests.AnyAsync(x => x.Id != id && x.Code != null && EF.Functions.ILike(x.Code, code)))
            return Conflict("Test code must be unique.");

        e.Name = dto.Name.Trim();
        e.DisplayName = dto.DisplayName?.Trim();
        e.Code = code;
        e.DepartmentId = dto.DepartmentId;
        e.UnitId = dto.UnitId;
        e.ValueType = dto.ValueType;
        e.IsActive = dto.IsActive;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            var inner = ex.InnerException?.Message ?? string.Empty;
            if (inner.Contains("FK_tests_departments_DepartmentId", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Selected department does not exist.");
            if (inner.Contains("FK_tests_units_UnitId", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Selected unit does not exist.");
            if (inner.Contains("IX_tests_Code", StringComparison.OrdinalIgnoreCase)
                || inner.Contains("tests_code", StringComparison.OrdinalIgnoreCase)
                || (inner.Contains("unique", StringComparison.OrdinalIgnoreCase) && inner.Contains("Code", StringComparison.OrdinalIgnoreCase)))
                return Conflict("Test code must be unique.");
            return Problem(detail: "Unable to update test.", statusCode: 500);
        }

        return new TestDto(e.Id, e.Name, e.DisplayName, e.Code, e.DepartmentId, e.UnitId, e.ValueType, e.IsActive);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTest(int id)
    {
        var e = await db.Tests.FindAsync(id);
        if (e is null) return NotFound();
        db.Tests.Remove(e);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] bool isActive)
    {
        var e = await db.Tests.FindAsync(id);
        if (e is null) return NotFound();
        e.IsActive = isActive;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // --- Parameters ---
    [HttpGet("{testId:int}/parameters")]
    public async Task<IActionResult> ListParameters(int testId)
    {
        var items = await db.TestParameters.AsNoTracking()
            .Where(p => p.TestId == testId)
            .OrderBy(p => p.SortOrder)
            .Select(p => new TestParameterDto(p.Id, p.TestId, p.Name, p.UnitId, p.SortOrder, p.IsActive))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost("{testId:int}/parameters")]
    public async Task<ActionResult<TestParameterDto>> CreateParameter(int testId, [FromBody] TestParameterCreateDto dto)
    {
        if (testId != dto.TestId) return BadRequest("Mismatched testId.");
        var e = new TestParameter { TestId = dto.TestId, Name = dto.Name.Trim(), UnitId = dto.UnitId, SortOrder = dto.SortOrder, IsActive = dto.IsActive };
        db.TestParameters.Add(e);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetParameter), new { testId, paramId = e.Id }, new TestParameterDto(e.Id, e.TestId, e.Name, e.UnitId, e.SortOrder, e.IsActive));
    }

    [HttpGet("{testId:int}/parameters/{paramId:int}")]
    public async Task<ActionResult<TestParameterDto>> GetParameter(int testId, int paramId)
    {
        var p = await db.TestParameters.FindAsync(paramId);
        if (p is null || p.TestId != testId) return NotFound();
        return new TestParameterDto(p.Id, p.TestId, p.Name, p.UnitId, p.SortOrder, p.IsActive);
    }

    [HttpPut("{testId:int}/parameters/{paramId:int}")]
    public async Task<ActionResult<TestParameterDto>> UpdateParameter(int testId, int paramId, [FromBody] TestParameterUpdateDto dto)
    {
        var p = await db.TestParameters.FindAsync(paramId);
        if (p is null || p.TestId != testId) return NotFound();
        p.Name = dto.Name.Trim();
        p.UnitId = dto.UnitId;
        p.SortOrder = dto.SortOrder;
        p.IsActive = dto.IsActive;
        await db.SaveChangesAsync();
        return new TestParameterDto(p.Id, p.TestId, p.Name, p.UnitId, p.SortOrder, p.IsActive);
    }

    [HttpDelete("{testId:int}/parameters/{paramId:int}")]
    public async Task<IActionResult> DeleteParameter(int testId, int paramId)
    {
        var p = await db.TestParameters.FindAsync(paramId);
        if (p is null || p.TestId != testId) return NotFound();
        db.TestParameters.Remove(p);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // --- Reference Ranges ---
    [HttpGet("parameters/{paramId:int}/ranges")]
    public async Task<IActionResult> ListRanges(int paramId)
    {
        var items = await db.TestReferenceRanges.AsNoTracking()
            .Where(r => r.TestParameterId == paramId)
            .OrderBy(r => r.SpeciesId).ThenBy(r => r.AgeFromDays).ThenBy(r => r.AgeToDays)
            .Select(r => new TestReferenceRangeDto(r.Id, r.TestParameterId, r.SpeciesId, r.AgeFromDays, r.AgeToDays, r.Low, r.High, r.Note, r.IsActive))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost("parameters/{paramId:int}/ranges")]
    public async Task<ActionResult<TestReferenceRangeDto>> CreateRange(int paramId, [FromBody] TestReferenceRangeCreateDto dto)
    {
        if (paramId != dto.TestParameterId) return BadRequest("Mismatched parameter id.");
        var e = new TestReferenceRange
        {
            TestParameterId = dto.TestParameterId,
            SpeciesId = dto.SpeciesId,
            AgeFromDays = dto.AgeFromDays,
            AgeToDays = dto.AgeToDays,
            Low = dto.Low, High = dto.High,
            Note = dto.Note, IsActive = dto.IsActive
        };
        db.TestReferenceRanges.Add(e);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetRange), new { paramId, rangeId = e.Id },
            new TestReferenceRangeDto(e.Id, e.TestParameterId, e.SpeciesId, e.AgeFromDays, e.AgeToDays, e.Low, e.High, e.Note, e.IsActive));
    }

    [HttpGet("parameters/{paramId:int}/ranges/{rangeId:int}")]
    public async Task<ActionResult<TestReferenceRangeDto>> GetRange(int paramId, int rangeId)
    {
        var r = await db.TestReferenceRanges.FindAsync(rangeId);
        if (r is null || r.TestParameterId != paramId) return NotFound();
        return new TestReferenceRangeDto(r.Id, r.TestParameterId, r.SpeciesId, r.AgeFromDays, r.AgeToDays, r.Low, r.High, r.Note, r.IsActive);
    }

    [HttpPut("parameters/{paramId:int}/ranges/{rangeId:int}")]
    public async Task<ActionResult<TestReferenceRangeDto>> UpdateRange(int paramId, int rangeId, [FromBody] TestReferenceRangeUpdateDto dto)
    {
        var r = await db.TestReferenceRanges.FindAsync(rangeId);
        if (r is null || r.TestParameterId != paramId) return NotFound();

        // Optional: enforce unique (paramId, species, age band)
        var dup = await db.TestReferenceRanges.AnyAsync(x =>
            x.Id != rangeId && x.TestParameterId == paramId && x.SpeciesId == dto.SpeciesId &&
            x.AgeFromDays == dto.AgeFromDays && x.AgeToDays == dto.AgeToDays);
        if (dup) return Conflict("Range for same species and age band already exists.");

        r.SpeciesId = dto.SpeciesId;
        r.AgeFromDays = dto.AgeFromDays;
        r.AgeToDays = dto.AgeToDays;
        r.Low = dto.Low;
        r.High = dto.High;
        r.Note = dto.Note;
        r.IsActive = dto.IsActive;
        await db.SaveChangesAsync();

        return new TestReferenceRangeDto(r.Id, r.TestParameterId, r.SpeciesId, r.AgeFromDays, r.AgeToDays, r.Low, r.High, r.Note, r.IsActive);
    }

    [HttpDelete("parameters/{paramId:int}/ranges/{rangeId:int}")]
    public async Task<IActionResult> DeleteRange(int paramId, int rangeId)
    {
        var r = await db.TestReferenceRanges.FindAsync(rangeId);
        if (r is null || r.TestParameterId != paramId) return NotFound();
        db.TestReferenceRanges.Remove(r);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
