namespace VetLims.Infrastructure.Entities;

public class ClientRate : BaseEntity
{
    public int ClientId { get; set; }
    public Client Client { get; set; } = default!;

    public int? ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public int ItemId { get; set; }

    // Use the single enum defined in RateItemType.cs
    public RegItemType ItemType { get; set; }

    public decimal Rate { get; set; }
    public string? Currency { get; set; }
    public bool IsActive { get; set; } = true;
}
