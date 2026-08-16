namespace VetLims.Infrastructure.Entities;

public class BranchTest : BaseEntity, IOrganizationOwned
{
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = default!;

    public int TestId { get; set; }
    public Test Test { get; set; } = default!;

    public bool IsActive { get; set; } = true;
}
