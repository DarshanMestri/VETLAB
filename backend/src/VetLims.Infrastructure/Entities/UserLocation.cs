namespace VetLims.Infrastructure.Entities;

public class UserLocation : BaseEntity, IOrganizationOwned
{
    public int UserId { get; set; }
    public global::VetLims.Infrastructure.Identity.AppUser? User { get; set; }

    // Branch represents a location
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    // Whether this location is the user's default selected location
    public bool IsDefault { get; set; } = false;

    // Explicitly enable/disable this branch assignment without deleting it.
    public bool IsActive { get; set; } = true;

    // Per-branch access control
    public bool HasClinicAccess { get; set; } = true;
    public bool HasLaboratoryAccess { get; set; } = true;
}
