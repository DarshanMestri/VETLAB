using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;
using VetLims.Infrastructure.Services;
using VetLims.WebApi.DTOs;

namespace VetLims.WebApi.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
public class BranchTestsController(VetLimsDbContext db, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet("api/branch-tests")]
    public async Task<ActionResult<IEnumerable<BranchTestDto>>> ListForOrganization()
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var items = await db.BranchTests.AsNoTracking()
            .Include(x => x.Branch)
            .ThenInclude(x => x.Clinic)
            .Include(x => x.Test)
            .Where(x => x.Branch.Clinic.OrganizationId == organizationId.Value && !x.IsDeleted && !x.Test.IsDeleted)
            .OrderBy(x => x.BranchId)
            .ThenBy(x => x.Test.Name)
            .Select(x => new BranchTestDto(
                x.Id,
                x.BranchId,
                x.TestId,
                x.Test.Name,
                x.Test.Code,
                x.IsActive))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("api/branch-tests/{id:int}")]
    public async Task<ActionResult<BranchTestDto>> Get(int id)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var mapping = await db.BranchTests.AsNoTracking()
            .Include(x => x.Branch)
            .ThenInclude(x => x.Clinic)
            .Include(x => x.Test)
            .FirstOrDefaultAsync(x => x.Id == id && x.Branch.Clinic.OrganizationId == organizationId.Value && !x.IsDeleted && !x.Test.IsDeleted);

        if (mapping is null)
            return NotFound();

        return Ok(new BranchTestDto(mapping.Id, mapping.BranchId, mapping.TestId, mapping.Test.Name, mapping.Test.Code, mapping.IsActive));
    }

    [HttpGet("api/branches/{branchId:int}/tests")]
    public async Task<ActionResult<IEnumerable<BranchTestDto>>> List(int branchId)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var branch = await db.Branches.AsNoTracking()
            .Include(x => x.Clinic)
            .FirstOrDefaultAsync(x => x.Id == branchId && x.Clinic.OrganizationId == organizationId.Value && !x.IsDeleted);

        if (branch is null)
            return NotFound();

        var items = await db.BranchTests.AsNoTracking()
            .Include(x => x.Test)
            .Where(x => x.BranchId == branchId && !x.IsDeleted && !x.Test.IsDeleted)
            .OrderBy(x => x.Test.Name)
            .Select(x => new BranchTestDto(
                x.Id,
                x.BranchId,
                x.TestId,
                x.Test.Name,
                x.Test.Code,
                x.IsActive))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("api/branches/{branchId:int}/tests/available")]
    public async Task<ActionResult<IEnumerable<TestDto>>> Available(int branchId)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var branch = await db.Branches.AsNoTracking()
            .Include(x => x.Clinic)
            .FirstOrDefaultAsync(x => x.Id == branchId && x.Clinic.OrganizationId == organizationId.Value && !x.IsDeleted);

        if (branch is null)
            return NotFound();

        var alreadyAssigned = await db.BranchTests.AsNoTracking()
            .Where(x => x.BranchId == branchId && !x.IsDeleted)
            .Select(x => x.TestId)
            .ToListAsync();

        var items = await db.Tests.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId.Value && !x.IsDeleted && x.IsActive && !alreadyAssigned.Contains(x.Id))
            .OrderBy(x => x.Name)
            .Select(x => new TestDto(x.Id, x.Name, x.DisplayName, x.Code, x.DepartmentId, x.UnitId, x.SampleTypeId, x.ValueType, x.IsActive))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("api/branches/{branchId:int}/tests/{testId:int}")]
    public async Task<ActionResult<BranchTestDto>> Assign(int branchId, int testId)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var branch = await db.Branches.Include(x => x.Clinic)
            .FirstOrDefaultAsync(x => x.Id == branchId && x.Clinic.OrganizationId == organizationId.Value && !x.IsDeleted);

        if (branch is null)
            return NotFound("Branch not found.");

        var test = await db.Tests.FirstOrDefaultAsync(x => x.Id == testId && x.OrganizationId == organizationId.Value && !x.IsDeleted);
        if (test is null)
            return NotFound("Test not found.");

        if (!test.IsActive)
            return BadRequest("Test is inactive and cannot be assigned.");

        if (await db.BranchTests.AnyAsync(x => x.BranchId == branchId && x.TestId == testId && !x.IsDeleted))
            return Conflict("Test is already assigned to this branch.");

        var assignment = new BranchTest
        {
            BranchId = branchId,
            TestId = testId,
            IsActive = true
        };

        db.BranchTests.Add(assignment);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(List), new { branchId }, new BranchTestDto(
            assignment.Id,
            assignment.BranchId,
            assignment.TestId,
            test.Name,
            test.Code,
            assignment.IsActive));
    }

    [HttpPut("api/branch-tests/{id:int}")]
    public async Task<ActionResult<BranchTestDto>> Update(int id, [FromBody] bool isActive)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var mapping = await db.BranchTests
            .Include(x => x.Branch)
            .ThenInclude(x => x.Clinic)
            .Include(x => x.Test)
            .FirstOrDefaultAsync(x => x.Id == id && x.Branch.Clinic.OrganizationId == organizationId.Value && !x.IsDeleted && !x.Test.IsDeleted);

        if (mapping is null)
            return NotFound();

        mapping.IsActive = isActive;
        await db.SaveChangesAsync();

        return Ok(new BranchTestDto(mapping.Id, mapping.BranchId, mapping.TestId, mapping.Test.Name, mapping.Test.Code, mapping.IsActive));
    }

    [HttpDelete("api/branch-tests/{id:int}")]
    public async Task<IActionResult> Remove(int id)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var mapping = await db.BranchTests
            .Include(x => x.Branch)
            .ThenInclude(x => x.Clinic)
            .FirstOrDefaultAsync(x => x.Id == id && x.Branch.Clinic.OrganizationId == organizationId.Value && !x.IsDeleted);

        if (mapping is null)
            return NotFound();

        mapping.IsDeleted = true;
        mapping.DeletedDate = DateTime.UtcNow;
        mapping.IsActive = false;
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("api/branch-tests/{id:int}/status")]
    public async Task<IActionResult> ToggleStatus(int id, [FromBody] bool isActive)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var mapping = await db.BranchTests
            .Include(x => x.Branch)
            .ThenInclude(x => x.Clinic)
            .FirstOrDefaultAsync(x => x.Id == id && x.Branch.Clinic.OrganizationId == organizationId.Value && !x.IsDeleted);

        if (mapping is null)
            return NotFound();

        mapping.IsActive = isActive;
        await db.SaveChangesAsync();

        return NoContent();
    }

    private Task<int?> ResolveCurrentOrganizationIdAsync()
    {
        var orgId = currentUserService.OrganizationId;
        if (!orgId.HasValue)
            return Task.FromResult<int?>(null);

        return Task.FromResult<int?>(orgId.Value);
    }
}

public record BranchTestDto(int Id, int BranchId, int TestId, string TestName, string? TestCode, bool IsActive);
