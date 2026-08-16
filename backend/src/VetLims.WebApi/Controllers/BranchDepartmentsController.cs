using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;
using VetLims.WebApi.DTOs;

namespace VetLims.WebApi.Controllers;

[ApiController]
[Route("api/branches/{branchId:int}/departments")]
[Authorize(Roles = "Admin")]
public class BranchDepartmentsController(VetLimsDbContext db, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BranchDepartmentDto>>> List(int branchId)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var branch = await db.Branches.AsNoTracking()
            .Include(x => x.Clinic)
            .FirstOrDefaultAsync(x => x.Id == branchId && x.Clinic.OrganizationId == organizationId.Value && !x.IsDeleted);

        if (branch is null)
            return NotFound();

        var items = await db.BranchDepartments.AsNoTracking()
            .Include(x => x.Department)
            .Where(x => x.BranchId == branchId && !x.IsDeleted && !x.Department.IsDeleted)
            .OrderBy(x => x.Department.Name)
            .Select(x => new BranchDepartmentDto(
                x.Id,
                x.BranchId,
                x.DepartmentId,
                x.Department.Name,
                x.Department.Code,
                x.IsActive))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("available")]
    public async Task<ActionResult<IEnumerable<DepartmentDto>>> Available(int branchId)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var branch = await db.Branches.AsNoTracking()
            .Include(x => x.Clinic)
            .FirstOrDefaultAsync(x => x.Id == branchId && x.Clinic.OrganizationId == organizationId.Value && !x.IsDeleted);

        if (branch is null)
            return NotFound();

        var alreadyAssigned = await db.BranchDepartments.AsNoTracking()
            .Where(x => x.BranchId == branchId && !x.IsDeleted)
            .Select(x => x.DepartmentId)
            .ToListAsync();

        var items = await db.Departments.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive && !alreadyAssigned.Contains(x.Id))
            .OrderBy(x => x.Name)
            .Select(x => new DepartmentDto(x.Id, x.Name, x.Code, x.IsActive))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("{departmentId:int}")]
    public async Task<ActionResult<BranchDepartmentDto>> Assign(int branchId, int departmentId)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var branch = await db.Branches.Include(x => x.Clinic)
            .FirstOrDefaultAsync(x => x.Id == branchId && x.Clinic.OrganizationId == organizationId.Value && !x.IsDeleted);

        if (branch is null)
            return NotFound();

        var department = await db.Departments.FirstOrDefaultAsync(x => x.Id == departmentId && !x.IsDeleted);
        if (department is null)
            return NotFound("Department not found.");

        if (!department.IsActive)
            return BadRequest("Department is inactive and cannot be assigned.");

        if (await db.BranchDepartments.AnyAsync(x => x.BranchId == branchId && x.DepartmentId == departmentId && !x.IsDeleted))
            return Conflict("Department is already assigned to this branch.");

        var assignment = new BranchDepartment
        {
            BranchId = branchId,
            DepartmentId = departmentId,
            IsActive = true
        };

        db.BranchDepartments.Add(assignment);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(List), new { branchId }, new BranchDepartmentDto(
            assignment.Id,
            assignment.BranchId,
            assignment.DepartmentId,
            department.Name,
            department.Code,
            assignment.IsActive));
    }

    [HttpDelete("{departmentId:int}")]
    public async Task<IActionResult> Remove(int branchId, int departmentId)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var branch = await db.Branches.Include(x => x.Clinic)
            .FirstOrDefaultAsync(x => x.Id == branchId && x.Clinic.OrganizationId == organizationId.Value && !x.IsDeleted);

        if (branch is null)
            return NotFound();

        var assignment = await db.BranchDepartments
            .FirstOrDefaultAsync(x => x.BranchId == branchId && x.DepartmentId == departmentId && !x.IsDeleted);

        if (assignment is null)
            return NotFound();

        assignment.IsDeleted = true;
        assignment.DeletedDate = DateTime.UtcNow;
        assignment.IsActive = false;
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{departmentId:int}/status")]
    public async Task<IActionResult> ToggleStatus(int branchId, int departmentId, [FromBody] bool isActive)
    {
        var organizationId = await ResolveCurrentOrganizationIdAsync();
        if (!organizationId.HasValue)
            return Forbid();

        var branch = await db.Branches.Include(x => x.Clinic)
            .FirstOrDefaultAsync(x => x.Id == branchId && x.Clinic.OrganizationId == organizationId.Value && !x.IsDeleted);

        if (branch is null)
            return NotFound();

        var assignment = await db.BranchDepartments
            .FirstOrDefaultAsync(x => x.BranchId == branchId && x.DepartmentId == departmentId && !x.IsDeleted);

        if (assignment is null)
            return NotFound();

        assignment.IsActive = isActive;
        assignment.ModifiedDate = DateTime.UtcNow;
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
