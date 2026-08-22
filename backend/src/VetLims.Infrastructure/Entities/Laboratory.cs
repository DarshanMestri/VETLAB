namespace VetLims.Infrastructure.Entities;

public class Laboratory : BaseEntity, IOrganizationOwned
{
    public Organization Organization { get; set; } = default!;


    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    // Additional laboratory master fields
    public string? ShortName { get; set; }
    public string? LicenseNumber { get; set; }
    public string? Gst { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Address { get; set; }
    public string? Logo { get; set; }
    public string? DigitalSignature { get; set; }
    public string? ReportHeader { get; set; }
    public string? ReportFooter { get; set; }
    public string? WhatsAppNumber { get; set; }
    public string? RegistrationNumber { get; set; }

    // Per-laboratory settings
    public string? EmailSettingsJson { get; set; }   // store SMTP/SendGrid settings as JSON
    public string? Timezone { get; set; }
    public string? Currency { get; set; }
    public string? Language { get; set; }

    public bool IsActive { get; set; } = true;
}
