namespace VetLims.Infrastructure.Entities;

// High-level grouping for tests (e.g., Hematology, Biochemistry)
public class Department : BaseEntity
{
    public string Name { get; set; } = default!;  // any language
    public string? Code { get; set; }             // optional short code
    public bool IsActive { get; set; } = true;
}
