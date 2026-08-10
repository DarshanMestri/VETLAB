namespace VetLims.Infrastructure.Entities;

// A named collection of Tests (e.g., "Liver Profile")
public class Profile : BaseEntity
{
    public string Code { get; set; } = default!;   // any language
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;

    public ICollection<ProfileTest> ProfileTests { get; set; } = new List<ProfileTest>();
}

// Join table between Profile and Test (composite PK)
public class ProfileTest
{
    public int ProfileId { get; set; }
    public Profile Profile { get; set; } = default!;

    public int TestId { get; set; }
    public Test Test { get; set; } = default!;

    public int DisplayOrder { get; set; } = 0;
}
