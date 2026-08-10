namespace VetLims.Infrastructure.Entities;

// A Package can contain Tests and/or entire Profiles
public class Package : BaseEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;

    public ICollection<PackageItem> Items { get; set; } = new List<PackageItem>();
}

public enum PackageItemType
{
    Test = 1,
    Profile = 2
}

public class PackageItem : BaseEntity
{
    public int PackageId { get; set; }
    public Package Package { get; set; } = default!;

    public PackageItemType ItemType { get; set; }  // Test/Profile
    public int ItemId { get; set; }                // TestId OR ProfileId
    public int DisplayOrder { get; set; } = 0;
}
