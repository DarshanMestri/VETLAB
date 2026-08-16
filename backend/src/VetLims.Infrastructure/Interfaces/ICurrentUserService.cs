namespace VetLims.Infrastructure;

public interface ICurrentUserService
{
    // True only for an explicitly established trusted system/background scope.
    bool IsSystemExecution { get; }

    int? UserId { get; }
    string? UserName { get; }

    // Tenant context extracted from JWT
    int? OrganizationId { get; }
    int? ClinicId { get; }
    int? BranchId { get; }

    // If user has multiple assigned branches, this will contain them
    IReadOnlyCollection<int> BranchIds { get; }

    // Roles present on the current user
    IReadOnlyCollection<string> Roles { get; }
}
