namespace VetLims.Infrastructure.Entities;

public enum BillingModuleType
{
    Consultation = 1,
    Laboratory = 2,
    Pharmacy = 3,
    Surgery = 4,
    Hospitalization = 5
}

public enum BillingInvoiceStatus
{
    Draft = 1,
    Finalized = 2,
    Cancelled = 3
}

public enum BillingTransactionType
{
    Payment = 1,
    Refund = 2,
    Credit = 3
}

public class BillingInvoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public BillingInvoiceStatus Status { get; set; } = BillingInvoiceStatus.Draft;

    public int? OwnerId { get; set; }
    public Owner? Owner { get; set; }

    public int? AnimalId { get; set; }
    public Animal? Animal { get; set; }

    public int? DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    public int? RegistrationId { get; set; }
    public Registration? Registration { get; set; }

    public int? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public string Currency { get; set; } = "AED";
    public string? Notes { get; set; }

    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal PaymentAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal CreditBalanceAmount { get; set; }

    public ICollection<BillingInvoiceLine> Lines { get; set; } = new List<BillingInvoiceLine>();
    public ICollection<BillingTransaction> Transactions { get; set; } = new List<BillingTransaction>();
}

public class BillingInvoiceLine : BaseEntity
{
    public int BillingInvoiceId { get; set; }
    public BillingInvoice BillingInvoice { get; set; } = default!;

    public BillingModuleType ModuleType { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class BillingTransaction : BaseEntity
{
    public int BillingInvoiceId { get; set; }
    public BillingInvoice BillingInvoice { get; set; } = default!;

    public BillingTransactionType TransactionType { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactedAt { get; set; } = DateTime.UtcNow;
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}
