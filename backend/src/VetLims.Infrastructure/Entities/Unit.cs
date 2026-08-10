namespace VetLims.Infrastructure.Entities;

// Units for test parameters (mg/dL, g/L, etc.)
public class Unit : BaseEntity
{
    public string Name { get; set; } = default!;     // e.g., "mg/dL"
    public string? Symbol {  get; set; }             // e.g., "mg/dL" or "g/L"
    public bool IsActive { get; set; } = true;
}
