namespace VetLims.Infrastructure.Entities;

public class Rate : BaseEntity
{
    public int? ClientId { get; set; }     // null = default/global
    public Client? Client { get; set; }

    public int? ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public RegItemType ItemType { get; set; }
    public int ItemId { get; set; }

    public decimal Price { get; set; }
    public string Currency { get; set; } = "AED";
    public bool IsActive { get; set; } = true;
}
