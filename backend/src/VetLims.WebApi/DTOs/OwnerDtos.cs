namespace VetLims.WebApi.DTOs;

// Step 6: Owner, animal and longitudinal medical-history API contracts.
public record OwnerDto(int Id, string Code, string Name, string? NameAr, string? Mobile, string? WhatsApp, string? Email, string? Address, string? City, bool IsActive, int AnimalCount);
public record OwnerUpsertDto(string? Code, string Name, string? NameAr, string? Mobile, string? WhatsApp, string? Email, string? Address, string? City, bool IsActive = true);
public record OwnerProfileDto(int Id, string Code, string Name, string? NameAr, string? Mobile, string? WhatsApp, string? Email, string? Address, string? City, bool IsActive, IReadOnlyList<AnimalSummaryDto> Animals);

public record AnimalSummaryDto(int Id, string Code, string Name, int SpeciesId, string SpeciesName, DateOnly? DateOfBirth, int? AgeInDays, decimal? WeightKg, string? VaccinationStatus, bool IsActive);
public record AnimalProfileDto(int Id, int OwnerId, string OwnerName, string Code, string Name, int SpeciesId, string SpeciesName, int? BreedId, string? BreedName, int? GenderId, string? GenderName, int? ColorId, string? ColorName, DateOnly? DateOfBirth, int? AgeInDays, decimal? WeightKg, string? MicrochipNumber, string? PhotoUrl, string? VaccinationStatus, bool IsActive, IReadOnlyList<MedicalHistoryDto> MedicalHistory);
public record AnimalUpsertDto(string? Code, string Name, int SpeciesId, int? BreedId, int? GenderId, int? ColorId, DateOnly? DateOfBirth, decimal? WeightKg, string? MicrochipNumber, string? PhotoUrl, string? VaccinationStatus, bool IsActive = true);

public record MedicalHistoryDto(int Id, DateTime OccurredAt, string EntryType, string? Diagnosis, string? Treatment, string? Medication, string? Notes);
public record MedicalHistoryUpsertDto(DateTime OccurredAt, string EntryType, string? Diagnosis, string? Treatment, string? Medication, string? Notes);

public record LookupItemDto(int Id, string Code, string Name);
public record OwnerAnimalLookupsDto(
    IReadOnlyList<LookupItemDto> Species,
    IReadOnlyList<LookupItemDto> Breeds,
    IReadOnlyList<LookupItemDto> Genders,
    IReadOnlyList<LookupItemDto> Colors);
