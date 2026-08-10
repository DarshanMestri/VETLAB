namespace VetLims.Infrastructure.Entities;

public class UserLocation : BaseEntity
{
    public int UserId { get; set; }
    public global::VetLims.Infrastructure.Identity.AppUser? User { get; set; }

    // Branch represents a location
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    // Whether this location is the user's default selected location
    public bool IsDefault { get; set; } = false;
}