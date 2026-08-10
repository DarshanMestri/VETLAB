namespace VetLims.Infrastructure.Entities;

public class Owner : BaseEntity
{
    // Step 6: durable owner master. Registration.OwnerName remains unchanged for legacy compatibility.
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Animal> Animals { get; set; } = new List<Animal>();
}

/// <summary>Step 6: Durable veterinary patient record owned by exactly one owner.</summary>
public class Animal : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int OwnerId { get; set; }
    public Owner Owner { get; set; } = default!;
    public int SpeciesId { get; set; }
    public Species Species { get; set; } = default!;
    public int? BreedId { get; set; }
    public Breed? Breed { get; set; }
    public int? GenderId { get; set; }
    public Gender? Gender { get; set; }
    public int? ColorId { get; set; }
    public AnimalColor? Color { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public decimal? WeightKg { get; set; }
    public string? MicrochipNumber { get; set; }
    public string? PhotoUrl { get; set; }
    public string? VaccinationStatus { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<MedicalHistoryEntry> MedicalHistory { get; set; } = new List<MedicalHistoryEntry>();
}

/// <summary>Step 6: Immutable-style longitudinal clinical history entry for an animal.</summary>
public class MedicalHistoryEntry : BaseEntity
{
    public int AnimalId { get; set; }
    public Animal Animal { get; set; } = default!;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string EntryType { get; set; } = string.Empty;
    public string? Diagnosis { get; set; }
    public string? Treatment { get; set; }
    public string? Medication { get; set; }
    public string? Notes { get; set; }
}
