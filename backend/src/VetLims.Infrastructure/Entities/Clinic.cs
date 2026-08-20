namespace VetLims.Infrastructure.Entities;

public class Clinic : BaseEntity, IOrganizationOwned
{
    public Organization Organization { get; set; } = default!;

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Logo { get; set; }
    public string? LicenseNumber { get; set; }
    public string? ReportHeader { get; set; }
    public string? ReportFooter { get; set; }
    public string? DigitalSignature { get; set; }
    public bool IsActive { get; set; } = true;
}
