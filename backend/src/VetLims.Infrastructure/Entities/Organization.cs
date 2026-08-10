namespace VetLims.Infrastructure.Entities;

public class Organization : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }

    // New master fields for multi-organization support
    public string? RegistrationNumber { get; set; }
    public string? Website { get; set; }
    public string? Country { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
    public string? ZipOrPin { get; set; }

    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Logo { get; set; }

    public bool IsActive { get; set; } = true;
}
