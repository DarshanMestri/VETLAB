namespace VetLims.Infrastructure.Entities;

/// <summary>
/// Identifies entities whose persistence is required to be organization-scoped.
/// The OrganizationId member is inherited from BaseEntity and is not a new mapped property.
/// </summary>
public interface IOrganizationOwned
{
    int? OrganizationId { get; set; }
}
