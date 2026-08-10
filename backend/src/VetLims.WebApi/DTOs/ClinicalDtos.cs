using VetLims.Infrastructure.Entities;
namespace VetLims.WebApi.DTOs;

public record AppointmentCreateDto(int OwnerId, int AnimalId, int? DoctorId, AppointmentType Type, DateTime ScheduledAt, string? Reason, int? Priority = null);
public record AppointmentDto(int Id, int OwnerId, string OwnerName, int AnimalId, string AnimalName, int? DoctorId, string? DoctorName, AppointmentType Type, AppointmentStatus Status, DateTime ScheduledAt, string? Reason, int? QueueNumber, QueueStatus? QueueStatus, int? QueuePriority, bool HasEncounter);
public record QueueDto(int Id, int AppointmentId, int QueueNumber, DateTime QueuedAt, QueueStatus Status, int? Priority, string AnimalName, string OwnerName, AppointmentType AppointmentType, int? DoctorId, string? DoctorName);
public record EncounterUpsertDto(int? DoctorId, string? ChiefComplaint, string? History, string? Vitals, string? ClinicalExamination, string? Diagnosis, string? TreatmentPlan, DateTime? FollowUpAt, IReadOnlyList<PrescriptionUpsertDto>? Prescriptions, IReadOnlyList<VaccinationUpsertDto>? Vaccinations);
public record PrescriptionUpsertDto(int? MedicineId, string MedicineName, string? Dosage, string? Frequency, string? Duration, string? Instructions);
public record VaccinationUpsertDto(string VaccineName, DateOnly AdministeredOn, DateOnly? DueOn, string? BatchNumber, string? Notes);
public record VaccinationRecordDto(int Id, string VaccineName, DateOnly AdministeredOn, DateOnly? DueOn, string? BatchNumber, string? Notes);
public record EncounterDto(int Id, int AppointmentId, int? DoctorId, DateTime EncounteredAt, string? ChiefComplaint, string? History, string? Vitals, string? ClinicalExamination, string? Diagnosis, string? TreatmentPlan, DateTime? FollowUpAt, IReadOnlyList<PrescriptionUpsertDto> Prescriptions, IReadOnlyList<VaccinationRecordDto> Vaccinations);
public record ClinicalLookupAnimalDto(int Id, string Code, string Name);
public record ClinicalLookupOwnerDto(int Id, string Code, string Name, IReadOnlyList<ClinicalLookupAnimalDto> Animals);
public record ClinicalLookupDoctorDto(int Id, string Code, string Name);
public record ClinicalLookupMedicineDto(int Id, string Code, string Name);
public record ClinicalLookupsDto(
    IReadOnlyList<ClinicalLookupOwnerDto> Owners,
    IReadOnlyList<ClinicalLookupDoctorDto> Doctors,
    IReadOnlyList<ClinicalLookupMedicineDto> Medicines);
