namespace VetLims.Infrastructure.Entities;

public class LaboratoryGroup : BaseEntity, IOrganizationOwned
{
    public Organization Organization { get; set; } = default!;

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Laboratory> Laboratories { get; set; } = new List<Laboratory>();
}
