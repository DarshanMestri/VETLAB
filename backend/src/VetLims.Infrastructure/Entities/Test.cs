namespace VetLims.Infrastructure.Entities;

// A single measurable test, may have multiple parameters (e.g., "CBC" with Hb, WBC etc.)
public class Test : BaseEntity, IOrganizationOwned
{
    public string Name { get; set; } = default!;
    public string? Code { get; set; }
    public Organization Organization { get; set; } = default!;
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = default!;
    public string? DisplayName { get; set; }
    public int? UnitId { get; set; }
    public Unit? Unit { get; set; }
    public TestValueType ValueType { get; set; } = TestValueType.Text;
    public bool IsActive { get; set; } = true;

    // ✅ Needed by .WithMany(t => t.Parameters)
    public ICollection<TestParameter> Parameters { get; set; } = new List<TestParameter>();
}

public enum TestValueType
{
    Text = 1,
    Numeric = 2,
    Datetime = 3
}

// Parameter within a test (e.g., Hemoglobin inside CBC)
// public class TestParameters
// {
//     public int Id { get; set; }
//     public int TestId { get; set; }
//     public Test Test { get; set; } = default!;

//     public string Name { get; set; } = default!;   // e.g., "Hemoglobin"
//     public int? UnitId { get; set; }               // nullable if unitless
//     public Unit? Unit { get; set; }
//     public int SortOrder { get; set; }             // for UI ordering
//     public bool IsActive { get; set; } = true;

//     public ICollection<TestReferenceRange> ReferenceRanges { get; set; } = new List<TestReferenceRange>();
// }

// Reference ranges per species and (optional) age range
public class TestReferenceRange : BaseEntity
{
    public int TestParameterId { get; set; }
    public TestParameter TestParameter { get; set; } = default!;

    public int SpeciesId { get; set; }
    public Species Species { get; set; } = default!;

    // optional age band (store in days to keep it simple)
    public int? AgeFromDays { get; set; }   // e.g., 0
    public int? AgeToDays { get; set; }     // e.g., 365

    public decimal? Low { get; set; }
    public decimal? High { get; set; }
    public string? Note { get; set; }       // e.g., conditions/comments
    public bool IsActive { get; set; } = true;
}
