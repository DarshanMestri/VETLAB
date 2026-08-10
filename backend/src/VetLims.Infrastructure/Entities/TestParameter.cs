namespace VetLims.Infrastructure.Entities;

public class TestParameter : BaseEntity
{
    public int TestId { get; set; }
    public Test Test { get; set; } = default!;

    public string Name { get; set; } = default!;
    public int? UnitId { get; set; }
    public Unit? Unit { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    // ✅ Needed by .WithMany(p => p.ReferenceRanges)
    public ICollection<TestReferenceRange> ReferenceRanges { get; set; } = new List<TestReferenceRange>();
}
