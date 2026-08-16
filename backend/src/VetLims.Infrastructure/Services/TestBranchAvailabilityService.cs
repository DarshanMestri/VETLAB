using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure.Entities;

namespace VetLims.Infrastructure.Services;

public static class TestBranchAvailabilityService
{
    public static async Task<bool> CanUseTestAtBranchAsync(
        VetLimsDbContext db,
        int testId,
        int branchId,
        int organizationId,
        CancellationToken cancellationToken = default)
    {
        var test = await db.Tests.AsNoTracking()
            .Include(x => x.Department)
            .FirstOrDefaultAsync(x => x.Id == testId && x.OrganizationId == organizationId && !x.IsDeleted && x.IsActive, cancellationToken);

        if (test is null)
            return false;

        if (test.Department is null || test.Department.IsDeleted || !test.Department.IsActive)
            return false;

        var branch = await db.Branches.AsNoTracking()
            .Include(x => x.Clinic)
            .FirstOrDefaultAsync(x => x.Id == branchId && !x.IsDeleted && x.IsActive && x.Clinic != null && x.Clinic.OrganizationId == organizationId, cancellationToken);

        if (branch is null)
            return false;

        var branchDepartmentEnabled = await db.BranchDepartments.AsNoTracking()
            .AnyAsync(x => x.BranchId == branchId && x.DepartmentId == test.DepartmentId && !x.IsDeleted && x.IsActive, cancellationToken);

        if (!branchDepartmentEnabled)
            return false;

        var branchTestEnabled = await db.BranchTests.AsNoTracking()
            .AnyAsync(x => x.BranchId == branchId && x.TestId == testId && !x.IsDeleted && x.IsActive, cancellationToken);

        return branchTestEnabled;
    }

    public static async Task<(bool IsAllowed, string? Error)> ValidateTestUseAsync(
        VetLimsDbContext db,
        int testId,
        int branchId,
        int organizationId,
        CancellationToken cancellationToken = default)
    {
        if (!await CanUseTestAtBranchAsync(db, testId, branchId, organizationId, cancellationToken))
        {
            return (false, "The selected Test is not enabled for this Branch.");
        }

        return (true, null);
    }
}
