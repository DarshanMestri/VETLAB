namespace VetLims.Infrastructure.Entities;

public enum PaymentMethod { Cash = 1, Card = 2, Cheque = 3 }
public enum RegItemType { Test = 1, Profile = 2, Package = 3, Medicine = 4 }
public enum RegItemStatus { Pending = 1, InProgress = 2, Completed = 3, Canceled = 4 }
public enum SampleWorkflowStatus { NotCollected = 1, Collected = 2, Received = 3, Processing = 4, Processed = 5 }

public class Registration : BaseEntity
{
    public int? ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

    // Optional client (use common price list per client)
    public int? ClientId { get; set; }
    public Client? Client { get; set; }

    public int? OwnerId { get; set; }
    public Owner? Owner { get; set; }

    public int? DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    // Owner single name (free text, any language)
    public string OwnerName { get; set; } = default!;

    // Mobile captured at registration time (owner master mobile if existing owner)
    public string? OwnerMobile { get; set; }

    // Generated barcode for the registration header
    public string Barcode { get; set; } = default!;

    public string? ReferenceNumber { get; set; }
    public string? ClinicalNotes { get; set; }

    public ICollection<RegistrationAnimal> Animals { get; set; } = new List<RegistrationAnimal>();
    public ICollection<RegistrationItem> Items { get; set; } = new List<RegistrationItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public decimal TotalAmount { get; set; }     // sum of item prices (gross, includes VAT)
    public decimal TotalVat { get; set; }        // sum of item VAT amounts
    public decimal PaidAmount { get; set; }      // sum of payments
    public decimal Balance => TotalAmount - PaidAmount;
}

// public class RegistrationAnimal
// {
//     public int Id { get; set; }
//     public int RegistrationId { get; set; }
//     public Registration Registration { get; set; } = default!;

//     public int SpeciesId { get; set; }
//     public Species Species { get; set; } = default!;

//     // Optional pet/animal name
//     public string? AnimalName { get; set; }
// }

public class RegistrationItem : BaseEntity
{
    public int RegistrationId { get; set; }
    public Registration Registration { get; set; } = default!;

    public int? ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public RegItemType ItemType { get; set; }
    public int ItemId { get; set; }                 // TestId / ProfileId / PackageId (resolved by ItemType)

    public int? SpeciesId { get; set; }             // for per-species ref ranges if needed
    public Species? Species { get; set; }

    public int? SampleTypeId { get; set; }
    public SampleType? SampleType { get; set; }

    public decimal Price { get; set; }              // resolved from Rate Master at order time (net per unit, excludes VAT)
    public decimal VatRate { get; set; }            // VAT % applied at order time
    public decimal VatAmount { get; set; }          // line VAT = round(Price * VatRate / 100, 2) * Quantity
    public int Quantity { get; set; } = 1;          // how many animals this order applies to (species count or all animals)
    public RegItemStatus Status { get; set; } = RegItemStatus.Pending;
    public SampleWorkflowStatus SampleStatus { get; set; } = SampleWorkflowStatus.NotCollected;

    // Unique barcode per ordered item (for sample label / tracking)
    public string ItemBarcode { get; set; } = default!;
    public DateTime? SampleCollectedAt { get; set; }
    public DateTime? SampleReceivedAt { get; set; }
    public DateTime? SampleProcessedAt { get; set; }
    public string? SampleNotes { get; set; }
}

public class Payment : BaseEntity
{
    public int RegistrationId { get; set; }
    public Registration Registration { get; set; } = default!;

    public int? ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;

    // For cheque/card
    public string? Reference { get; set; }      // cheque no / POS txn id
    public string Currency { get; set; } = "AED";
}

// Simple sequential barcode source
