namespace VetLims.Infrastructure.Entities;

public enum AppointmentType { Scheduled = 1, WalkIn = 2, Emergency = 3 }
public enum AppointmentStatus { Scheduled = 1, CheckedIn = 2, InConsultation = 3, Completed = 4, Cancelled = 5, NoShow = 6 }
public enum QueueStatus { Waiting = 1, Called = 2, InConsultation = 3, Completed = 4, Cancelled = 5 }

/// <summary>Step 7: Clinical appointment, including walk-in and emergency registrations.</summary>
public class Appointment : BaseEntity
{
    public int OwnerId { get; set; }
    public Owner Owner { get; set; } = default!;
    public int AnimalId { get; set; }
    public Animal Animal { get; set; } = default!;
    public int? DoctorId { get; set; }
    public Doctor? Doctor { get; set; }
    public AppointmentType Type { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public DateTime ScheduledAt { get; set; }
    public string? Reason { get; set; }
    public bool IsActive { get; set; } = true;
    public QueueEntry? QueueEntry { get; set; }
    public ClinicalEncounter? Encounter { get; set; }
}

public class QueueEntry : BaseEntity
{
    public int AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = default!;
    public int QueueNumber { get; set; }
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public QueueStatus Status { get; set; } = QueueStatus.Waiting;
    public int? Priority { get; set; }
}

/// <summary>Step 7: Electronic medical record encounter.</summary>
public class ClinicalEncounter : BaseEntity
{
    public int AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = default!;
    public int? DoctorId { get; set; }
    public Doctor? Doctor { get; set; }
    public DateTime EncounteredAt { get; set; } = DateTime.UtcNow;
    public string? ChiefComplaint { get; set; }
    public string? History { get; set; }
    public string? Vitals { get; set; }
    public string? ClinicalExamination { get; set; }
    public string? Diagnosis { get; set; }
    public string? TreatmentPlan { get; set; }
    public DateTime? FollowUpAt { get; set; }
    public DiscountType? DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal DiscountAmount { get; set; }
    public ICollection<PrescriptionItem> Prescriptions { get; set; } = new List<PrescriptionItem>();
    public ICollection<VaccinationRecord> Vaccinations { get; set; } = new List<VaccinationRecord>();
}

public class PrescriptionItem : BaseEntity
{
    public int ClinicalEncounterId { get; set; }
    public ClinicalEncounter ClinicalEncounter { get; set; } = default!;
    public int? MedicineId { get; set; }
    public Medicine? Medicine { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? Dosage { get; set; }
    public string? Frequency { get; set; }
    public string? Duration { get; set; }
    public string? Instructions { get; set; }
}

public class VaccinationRecord : BaseEntity
{
    public int ClinicalEncounterId { get; set; }
    public ClinicalEncounter ClinicalEncounter { get; set; } = default!;
    public string VaccineName { get; set; } = string.Empty;
    public DateOnly AdministeredOn { get; set; }
    public DateOnly? DueOn { get; set; }
    public string? BatchNumber { get; set; }
    public string? Notes { get; set; }
}
