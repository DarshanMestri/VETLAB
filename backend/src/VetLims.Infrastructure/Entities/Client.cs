namespace VetLims.Infrastructure.Entities;

public class Client : BaseEntity
{
    // Supports any language; Postgres 'text' is unicode-friendly
    public string Name { get; set; } = default!;          // e.g., clinic/hospital/owner group
    public string? Code { get; set; }                     // optional short code
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}
