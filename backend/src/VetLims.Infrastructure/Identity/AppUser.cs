// Step 4: Identity user
using Microsoft.AspNetCore.Identity;
using VetLims.Infrastructure.Entities;

namespace VetLims.Infrastructure.Identity;

public class AppUser : IdentityUser<int>
{
    public string? DisplayName { get; set; }

    // Membership context
    public int? OrganizationId { get; set; }
    public int? BranchId { get; set; }

    public bool IsActive { get; set; } = true;

    public Organization? Organization { get; set; }
    public Branch? Branch { get; set; }

    // Support multiple assigned branches/locations per user
    public ICollection<UserLocation>? UserLocations { get; set; }
}
