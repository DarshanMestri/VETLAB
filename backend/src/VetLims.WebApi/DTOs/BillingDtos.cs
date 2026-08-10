using VetLims.Infrastructure.Entities;

namespace VetLims.WebApi.DTOs;

public class BillingListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public BillingInvoiceStatus? Status { get; set; }
    public BillingModuleType? ModuleType { get; set; }
}

public record BillingInvoiceListDto(
    int Id,
    string InvoiceNumber,
    DateTime InvoiceDate,
    BillingInvoiceStatus Status,
    string? OwnerName,
    string? AnimalName,
    string Currency,
    decimal NetAmount,
    decimal PaymentAmount,
    decimal RefundAmount,
    decimal CreditAmount,
    decimal OutstandingAmount,
    decimal CreditBalanceAmount
);

public record BillingInvoiceLineUpsertDto(
    BillingModuleType ModuleType,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount
);

public record BillingInvoiceCreateDto(
    DateTime? InvoiceDate,
    BillingInvoiceStatus Status,
    int? OwnerId,
    int? AnimalId,
    int? DoctorId,
    int? RegistrationId,
    int? AppointmentId,
    string Currency,
    decimal DiscountAmount,
    string? Notes,
    List<BillingInvoiceLineUpsertDto> Lines
);

public record BillingInvoiceUpdateDto(
    DateTime? InvoiceDate,
    BillingInvoiceStatus Status,
    int? OwnerId,
    int? AnimalId,
    int? DoctorId,
    int? RegistrationId,
    int? AppointmentId,
    string Currency,
    decimal DiscountAmount,
    string? Notes,
    List<BillingInvoiceLineUpsertDto> Lines
);

public record BillingInvoiceLineDto(
    int Id,
    BillingModuleType ModuleType,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal TotalAmount
);

public record BillingTransactionCreateDto(
    BillingTransactionType TransactionType,
    PaymentMethod Method,
    decimal Amount,
    DateTime? TransactedAt,
    string? Reference,
    string? Notes
);

public record BillingTransactionDto(
    int Id,
    BillingTransactionType TransactionType,
    PaymentMethod Method,
    decimal Amount,
    DateTime TransactedAt,
    string? Reference,
    string? Notes
);

public record BillingInvoiceDetailDto(
    int Id,
    string InvoiceNumber,
    DateTime InvoiceDate,
    BillingInvoiceStatus Status,
    int? OwnerId,
    string? OwnerName,
    int? AnimalId,
    string? AnimalName,
    int? DoctorId,
    string? DoctorName,
    int? RegistrationId,
    string? RegistrationBarcode,
    int? AppointmentId,
    string Currency,
    string? Notes,
    decimal SubtotalAmount,
    decimal DiscountAmount,
    decimal NetAmount,
    decimal PaymentAmount,
    decimal RefundAmount,
    decimal CreditAmount,
    decimal OutstandingAmount,
    decimal CreditBalanceAmount,
    IEnumerable<BillingInvoiceLineDto> Lines,
    IEnumerable<BillingTransactionDto> Transactions
);

public record BillingReceiptDto(
    int TransactionId,
    string InvoiceNumber,
    DateTime InvoiceDate,
    DateTime ReceiptDate,
    BillingTransactionType TransactionType,
    PaymentMethod Method,
    decimal Amount,
    string Currency,
    string? OwnerName,
    string? AnimalName,
    string? Reference,
    decimal NetAmount,
    decimal OutstandingAmount,
    decimal CreditBalanceAmount
);
