namespace VetLims.Infrastructure.Entities;

public class BranchDepartment : BaseEntity, IOrganizationOwned
{
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = default!;

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = default!;

    public bool IsActive { get; set; } = true;
}
