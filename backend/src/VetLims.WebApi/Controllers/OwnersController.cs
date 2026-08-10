using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;
using VetLims.WebApi.DTOs;

namespace VetLims.WebApi.Controllers;

/// <summary>Step 6: Durable owner/patient master and longitudinal medical history.</summary>
[ApiController]
[Route("api/owners")]
[Authorize]
public sealed class OwnersController(VetLimsDbContext db) : ControllerBase
{
    private const int MaximumPageSize = 100;

    [HttpGet("lookups")]
    public async Task<ActionResult<OwnerAnimalLookupsDto>> Lookups(CancellationToken ct = default)
    {
        var species = await db.Species.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new LookupItemDto(x.Id, x.Code, x.Name))
            .ToListAsync(ct);

        var breeds = await db.Breeds.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new LookupItemDto(x.Id, x.Code, x.Name))
            .ToListAsync(ct);

        var genders = await db.Genders.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new LookupItemDto(x.Id, x.Code, x.Name))
            .ToListAsync(ct);

        var colors = await db.AnimalColors.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new LookupItemDto(x.Id, x.Code, x.Name))
            .ToListAsync(ct);

        return Ok(new OwnerAnimalLookupsDto(species, breeds, genders, colors));
    }

    [HttpGet]
    public async Task<ActionResult<object>> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > MaximumPageSize) return BadRequest("Invalid page or page size.");
        var query = db.Owners.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Code, term) || EF.Functions.ILike(x.Name, term) || (x.Mobile != null && EF.Functions.ILike(x.Mobile, term)));
        }
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Code).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new OwnerDto(x.Id, x.Code, x.Name, x.Mobile, x.WhatsApp, x.Email, x.Address, x.City, x.IsActive, x.Animals.Count(a => !a.IsDeleted))).ToListAsync(ct);
        return Ok(new { total, items, page, pageSize });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<OwnerProfileDto>> Create(OwnerUpsertDto dto, CancellationToken ct = default)
    {
        var validation = await ValidateOwnerAsync(dto, null, ct); if (validation is not null) return validation;
        var owner = new Owner(); Apply(owner, dto); db.Owners.Add(owner); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { ownerId = owner.Id }, await ProjectOwnerAsync(owner.Id, ct));
    }

    [HttpGet("{ownerId:int}")]
    public async Task<ActionResult<OwnerProfileDto>> Get(int ownerId, CancellationToken ct = default)
    { var profile = await ProjectOwnerAsync(ownerId, ct); return profile is null ? NotFound() : Ok(profile); }

    [HttpPut("{ownerId:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int ownerId, OwnerUpsertDto dto, CancellationToken ct = default)
    {
        var owner = await db.Owners.FirstOrDefaultAsync(x => x.Id == ownerId && !x.IsDeleted, ct); if (owner is null) return NotFound();
        var validation = await ValidateOwnerAsync(dto, ownerId, ct); if (validation is not null) return validation;
        Apply(owner, dto); await db.SaveChangesAsync(ct); return Ok(await ProjectOwnerAsync(ownerId, ct));
    }

    [HttpDelete("{ownerId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int ownerId, CancellationToken ct = default)
    {
        var owner = await db.Owners.Include(x => x.Animals).FirstOrDefaultAsync(x => x.Id == ownerId && !x.IsDeleted, ct); if (owner is null) return NotFound();
        if (owner.Animals.Any(x => !x.IsDeleted)) return Conflict(new { message = "An owner with active animals cannot be deleted." });
        db.Owners.Remove(owner); await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpPost("{ownerId:int}/animals")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<AnimalProfileDto>> CreateAnimal(int ownerId, AnimalUpsertDto dto, CancellationToken ct = default)
    {
        if (!await db.Owners.AnyAsync(x => x.Id == ownerId && !x.IsDeleted, ct)) return NotFound();
        var validation = await ValidateAnimalAsync(dto, null, ct); if (validation is not null) return validation;
        var animal = new Animal { OwnerId = ownerId }; Apply(animal, dto); db.Animals.Add(animal); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetAnimal), new { ownerId, animalId = animal.Id }, await ProjectAnimalAsync(ownerId, animal.Id, ct));
    }

    [HttpGet("{ownerId:int}/animals/{animalId:int}")]
    public async Task<ActionResult<AnimalProfileDto>> GetAnimal(int ownerId, int animalId, CancellationToken ct = default)
    { var profile = await ProjectAnimalAsync(ownerId, animalId, ct); return profile is null ? NotFound() : Ok(profile); }

    [HttpPut("{ownerId:int}/animals/{animalId:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateAnimal(int ownerId, int animalId, AnimalUpsertDto dto, CancellationToken ct = default)
    {
        var animal = await db.Animals.FirstOrDefaultAsync(x => x.Id == animalId && x.OwnerId == ownerId && !x.IsDeleted, ct); if (animal is null) return NotFound();
        var validation = await ValidateAnimalAsync(dto, animalId, ct); if (validation is not null) return validation;
        Apply(animal, dto); await db.SaveChangesAsync(ct); return Ok(await ProjectAnimalAsync(ownerId, animalId, ct));
    }

    [HttpDelete("{ownerId:int}/animals/{animalId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAnimal(int ownerId, int animalId, CancellationToken ct = default)
    {
        var animal = await db.Animals.FirstOrDefaultAsync(x => x.Id == animalId && x.OwnerId == ownerId && !x.IsDeleted, ct); if (animal is null) return NotFound();
        db.Animals.Remove(animal); await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpPost("{ownerId:int}/animals/{animalId:int}/medical-history")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<MedicalHistoryDto>> AddHistory(int ownerId, int animalId, MedicalHistoryUpsertDto dto, CancellationToken ct = default)
    {
        if (!await AnimalExistsAsync(ownerId, animalId, ct)) return NotFound();
        var validation = ValidateHistory(dto); if (validation is not null) return validation;
        var entry = new MedicalHistoryEntry { AnimalId = animalId }; Apply(entry, dto); db.MedicalHistoryEntries.Add(entry); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetAnimal), new { ownerId, animalId }, Map(entry));
    }

    [HttpPut("{ownerId:int}/animals/{animalId:int}/medical-history/{entryId:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateHistory(int ownerId, int animalId, int entryId, MedicalHistoryUpsertDto dto, CancellationToken ct = default)
    {
        var entry = await db.MedicalHistoryEntries.FirstOrDefaultAsync(x => x.Id == entryId && x.AnimalId == animalId && !x.IsDeleted, ct); if (entry is null || !await AnimalExistsAsync(ownerId, animalId, ct)) return NotFound();
        var validation = ValidateHistory(dto); if (validation is not null) return validation;
        Apply(entry, dto); await db.SaveChangesAsync(ct); return Ok(Map(entry));
    }

    [HttpDelete("{ownerId:int}/animals/{animalId:int}/medical-history/{entryId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteHistory(int ownerId, int animalId, int entryId, CancellationToken ct = default)
    {
        var entry = await db.MedicalHistoryEntries.FirstOrDefaultAsync(x => x.Id == entryId && x.AnimalId == animalId && !x.IsDeleted, ct); if (entry is null || !await AnimalExistsAsync(ownerId, animalId, ct)) return NotFound();
        db.MedicalHistoryEntries.Remove(entry); await db.SaveChangesAsync(ct); return NoContent();
    }

    private async Task<ActionResult?> ValidateOwnerAsync(OwnerUpsertDto dto, int? id, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(dto.Code) || dto.Code.Trim().Length > 50) errors[nameof(dto.Code)] = ["Code is required and must be 50 characters or fewer."];
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Trim().Length > 200) errors[nameof(dto.Name)] = ["Name is required and must be 200 characters or fewer."];
        if (!string.IsNullOrWhiteSpace(dto.Email) && !new EmailAddressAttribute().IsValid(dto.Email)) errors[nameof(dto.Email)] = ["Email is invalid."];
        if (errors.Count > 0) return BadRequest(new ValidationProblemDetails(errors));
        if (await db.Owners.AnyAsync(x => x.Code == dto.Code.Trim() && !x.IsDeleted && x.Id != id, ct)) return Conflict(new { message = "Owner code already exists." });
        return null;
    }

    private async Task<ActionResult?> ValidateAnimalAsync(AnimalUpsertDto dto, int? id, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(dto.Code) || dto.Code.Trim().Length > 50) errors[nameof(dto.Code)] = ["Code is required and must be 50 characters or fewer."];
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Trim().Length > 200) errors[nameof(dto.Name)] = ["Name is required and must be 200 characters or fewer."];
        if (dto.SpeciesId <= 0 || !await db.Species.AnyAsync(x => x.Id == dto.SpeciesId && !x.IsDeleted, ct)) errors[nameof(dto.SpeciesId)] = ["A valid species is required."];
        if (dto.BreedId.HasValue && !await db.Breeds.AnyAsync(x => x.Id == dto.BreedId && !x.IsDeleted, ct)) errors[nameof(dto.BreedId)] = ["Breed does not exist."];
        if (dto.GenderId.HasValue && !await db.Genders.AnyAsync(x => x.Id == dto.GenderId && !x.IsDeleted, ct)) errors[nameof(dto.GenderId)] = ["Gender does not exist."];
        if (dto.ColorId.HasValue && !await db.AnimalColors.AnyAsync(x => x.Id == dto.ColorId && !x.IsDeleted, ct)) errors[nameof(dto.ColorId)] = ["Color does not exist."];
        if (dto.WeightKg is <= 0) errors[nameof(dto.WeightKg)] = ["Weight must be greater than zero."];
        if (errors.Count > 0) return BadRequest(new ValidationProblemDetails(errors));
        if (await db.Animals.AnyAsync(x => x.Code == dto.Code.Trim() && !x.IsDeleted && x.Id != id, ct)) return Conflict(new { message = "Animal code already exists." });
        if (!string.IsNullOrWhiteSpace(dto.MicrochipNumber) && await db.Animals.AnyAsync(x => x.MicrochipNumber == dto.MicrochipNumber.Trim() && !x.IsDeleted && x.Id != id, ct)) return Conflict(new { message = "Microchip number already exists." });
        return null;
    }

    private ActionResult? ValidateHistory(MedicalHistoryUpsertDto dto)
    {
        var errors = new Dictionary<string, string[]>();
        if (dto.OccurredAt == default || dto.OccurredAt > DateTime.UtcNow.AddMinutes(5)) errors[nameof(dto.OccurredAt)] = ["Occurrence date is required and cannot be in the future."];
        if (string.IsNullOrWhiteSpace(dto.EntryType) || dto.EntryType.Trim().Length > 100) errors[nameof(dto.EntryType)] = ["Entry type is required and must be 100 characters or fewer."];
        return errors.Count == 0 ? null : BadRequest(new ValidationProblemDetails(errors));
    }

    private async Task<bool> AnimalExistsAsync(int ownerId, int animalId, CancellationToken ct) => await db.Animals.AnyAsync(x => x.Id == animalId && x.OwnerId == ownerId && !x.IsDeleted, ct);
    // Age is calculated by the UI from the stored DOB; keeping the database projection provider-neutral for PostgreSQL.
    private async Task<OwnerProfileDto?> ProjectOwnerAsync(int ownerId, CancellationToken ct)
    {
        var owner = await db.Owners.AsNoTracking()
            .Where(x => x.Id == ownerId && !x.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.Mobile,
                x.WhatsApp,
                x.Email,
                x.Address,
                x.City,
                x.IsActive,
                Animals = x.Animals
                    .Where(a => !a.IsDeleted)
                    .OrderBy(a => a.Name)
                    .Select(a => new
                    {
                        a.Id,
                        a.Code,
                        a.Name,
                        a.SpeciesId,
                        SpeciesName = a.Species.Name,
                        a.DateOfBirth,
                        a.WeightKg,
                        a.VaccinationStatus,
                        a.IsActive
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        return owner is null
            ? null
            : new OwnerProfileDto(
                owner.Id,
                owner.Code,
                owner.Name,
                owner.Mobile,
                owner.WhatsApp,
                owner.Email,
                owner.Address,
                owner.City,
                owner.IsActive,
                owner.Animals
                    .Select(a => new AnimalSummaryDto(a.Id, a.Code, a.Name, a.SpeciesId, a.SpeciesName, a.DateOfBirth, CalculateAgeInDays(a.DateOfBirth), a.WeightKg, a.VaccinationStatus, a.IsActive))
                    .ToList());
    }

    private async Task<AnimalProfileDto?> ProjectAnimalAsync(int ownerId, int animalId, CancellationToken ct)
    {
        var animal = await db.Animals.AsNoTracking()
            .Where(x => x.Id == animalId && x.OwnerId == ownerId && !x.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.OwnerId,
                OwnerName = x.Owner.Name,
                x.Code,
                x.Name,
                x.SpeciesId,
                SpeciesName = x.Species.Name,
                x.BreedId,
                BreedName = x.Breed != null ? x.Breed.Name : null,
                x.GenderId,
                GenderName = x.Gender != null ? x.Gender.Name : null,
                x.ColorId,
                ColorName = x.Color != null ? x.Color.Name : null,
                x.DateOfBirth,
                x.WeightKg,
                x.MicrochipNumber,
                x.PhotoUrl,
                x.VaccinationStatus,
                x.IsActive,
                MedicalHistory = x.MedicalHistory
                    .Where(h => !h.IsDeleted)
                    .OrderByDescending(h => h.OccurredAt)
                    .Select(h => new MedicalHistoryDto(h.Id, h.OccurredAt, h.EntryType, h.Diagnosis, h.Treatment, h.Medication, h.Notes))
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        return animal is null
            ? null
            : new AnimalProfileDto(
                animal.Id,
                animal.OwnerId,
                animal.OwnerName,
                animal.Code,
                animal.Name,
                animal.SpeciesId,
                animal.SpeciesName,
                animal.BreedId,
                animal.BreedName,
                animal.GenderId,
                animal.GenderName,
                animal.ColorId,
                animal.ColorName,
                animal.DateOfBirth,
                CalculateAgeInDays(animal.DateOfBirth),
                animal.WeightKg,
                animal.MicrochipNumber,
                animal.PhotoUrl,
                animal.VaccinationStatus,
                animal.IsActive,
                animal.MedicalHistory);
    }
    private static MedicalHistoryDto Map(MedicalHistoryEntry x) => new(x.Id, x.OccurredAt, x.EntryType, x.Diagnosis, x.Treatment, x.Medication, x.Notes);
    private static void Apply(Owner x, OwnerUpsertDto dto) { x.Code = dto.Code.Trim(); x.Name = dto.Name.Trim(); x.Mobile = Clean(dto.Mobile); x.WhatsApp = Clean(dto.WhatsApp); x.Email = Clean(dto.Email); x.Address = Clean(dto.Address); x.City = Clean(dto.City); x.IsActive = dto.IsActive; }
    private static void Apply(Animal x, AnimalUpsertDto dto) { x.Code = dto.Code.Trim(); x.Name = dto.Name.Trim(); x.SpeciesId = dto.SpeciesId; x.BreedId = dto.BreedId; x.GenderId = dto.GenderId; x.ColorId = dto.ColorId; x.DateOfBirth = dto.DateOfBirth; x.WeightKg = dto.WeightKg; x.MicrochipNumber = Clean(dto.MicrochipNumber); x.PhotoUrl = Clean(dto.PhotoUrl); x.VaccinationStatus = Clean(dto.VaccinationStatus); x.IsActive = dto.IsActive; }
    private static void Apply(MedicalHistoryEntry x, MedicalHistoryUpsertDto dto) { x.OccurredAt = dto.OccurredAt; x.EntryType = dto.EntryType.Trim(); x.Diagnosis = Clean(dto.Diagnosis); x.Treatment = Clean(dto.Treatment); x.Medication = Clean(dto.Medication); x.Notes = Clean(dto.Notes); }
    private static int? CalculateAgeInDays(DateOnly? dateOfBirth)
    {
        if (dateOfBirth is null) return null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var ageInDays = today.DayNumber - dateOfBirth.Value.DayNumber;
        return ageInDays < 0 ? 0 : ageInDays;
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
