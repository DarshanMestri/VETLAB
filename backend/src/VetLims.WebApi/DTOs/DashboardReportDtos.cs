using VetLims.Infrastructure.Entities;

namespace VetLims.WebApi.DTOs;

public record DashboardKpiDto(string Label, decimal Value, string? Unit = null);
public record DashboardSectionDto(string Dashboard, IEnumerable<DashboardKpiDto> Kpis);

public class ReportQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public record RevenueReportRowDto(
    int InvoiceId,
    string InvoiceNumber,
    DateTime InvoiceDate,
    string? OwnerName,
    decimal NetAmount,
    decimal PaymentAmount,
    decimal OutstandingAmount,
    string Currency
);

public record RevenueReportSummaryDto(
    decimal NetAmount,
    decimal PaymentAmount,
    decimal OutstandingAmount
);

public record PendingReportRowDto(
    int RegistrationItemId,
    int RegistrationId,
    string RegistrationBarcode,
    string OwnerName,
    string? TestName,
    RegItemStatus Status,
    SampleWorkflowStatus SampleStatus,
    DateTime CreatedAt
);

public record TestStatisticDto(string TestName, int OrderedCount, int CompletedCount);
public record SpeciesStatisticDto(string SpeciesName, int RegistrationCount, int TestCount);

public record InventoryReportRowDto(
    int PurchaseLineId,
    string MedicineName,
    string BatchNumber,
    DateOnly? ExpiryDate,
    decimal Quantity,
    decimal AvailableQuantity,
    decimal LowStockThreshold,
    bool IsLowStock,
    bool IsExpired
);

public record AuditReportRowDto(
    int Id,
    string TableName,
    string RecordId,
    string Action,
    string? ChangedBy,
    DateTime ChangedAt
);
