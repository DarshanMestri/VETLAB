namespace VetLims.Infrastructure.Entities;

public enum ResultEntryType
{
    Numeric = 1,
    Text = 2,
    PositiveNegative = 3,
    Formula = 4,
    Image = 5,
    Pdf = 6
}

public class Result : BaseEntity
{
    public int RegistrationItemId { get; set; }           // the ordered test/profile/package item
    public RegistrationItem RegistrationItem { get; set; } = default!;

    public int? RegistrationAnimalId { get; set; }
    public RegistrationAnimal? RegistrationAnimal { get; set; }

    public int? LaboratoryId { get; set; }
    public Laboratory? Laboratory { get; set; }

    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }            // set when item is marked completed
    public string? Notes { get; set; }
    public string? TechnicianVerifiedBy { get; set; }
    public int? TechnicianVerifiedById { get; set; }
    public DateTime? TechnicianVerifiedAt { get; set; }
    public string? PathologistAuthenticatedBy { get; set; }
    public int? PathologistAuthenticatedById { get; set; }
    public DateTime? PathologistAuthenticatedAt { get; set; }
    public string? DigitalSignature { get; set; }
    public string? VerificationRemarks { get; set; }

    public ICollection<ResultValue> Values { get; set; } = new List<ResultValue>();
}

public class ResultValue : BaseEntity
{
    public int ResultId { get; set; }
    public Result Result { get; set; } = default!;

    public int? LaboratoryId { get; set; }
    public Laboratory? Laboratory { get; set; }

    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public int TestParameterId { get; set; }              // which parameter the value is for
    public TestParameter TestParameter { get; set; } = default!;

    // numeric result (if applicable)
    public decimal? NumericValue { get; set; }

    // text result (fallback, e.g., "Positive/Negative", "Smear OK")
    public string? TextValue { get; set; }
    public ResultEntryType EntryType { get; set; } = ResultEntryType.Numeric;
    public string? PositiveNegativeValue { get; set; }
    public string? FormulaExpression { get; set; }
    public string? FormulaResult { get; set; }
    public string? ImageUrl { get; set; }
    public string? PdfAttachmentUrl { get; set; }

    // snapshot for unit text at time of entry (so later unit name changes don't break reports)
    public string? UnitSymbol { get; set; }

    // computed flags using Ref Ranges (by species + ageDays)
    public bool? IsLow { get; set; }
    public bool? IsHigh { get; set; }
    public bool? IsOutOfRange { get; set; }

    // snapshot of reference range window used
    public int? RangeMinDays { get; set; }
    public int? RangeMaxDays { get; set; }
    public decimal? RangeLow { get; set; }
    public decimal? RangeHigh { get; set; }
}
