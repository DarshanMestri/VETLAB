namespace VetLims.Infrastructure.Entities;

public abstract class BaseEntity
{
    public int Id { get; set; }

    // Tenant/Audit context
    // Note: nullable to preserve backward compatibility for existing records.
    public int? OrganizationId { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedDate { get; set; }

    // Backward compatibility for existing models/controllers.
    public DateTime CreatedAt
    {
        get => CreatedDate;
        set => CreatedDate = value;
    }

    public DateTime? UpdatedAt
    {
        get => ModifiedDate;
        set => ModifiedDate = value;
    }
}
