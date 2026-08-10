namespace VetLims.Infrastructure.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Details { get; set; }
}
