using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;
using VetLims.WebApi.DTOs;

namespace VetLims.WebApi.Controllers;

/// <summary>Step 7: Clinical appointments, walk-ins/emergencies, queue and EMR encounters.</summary>
[ApiController, Route("api/clinical"), Authorize]
public class ClinicalController(VetLimsDbContext db) : ControllerBase
{
    [HttpGet("lookups")]
    public async Task<ActionResult<ClinicalLookupsDto>> GetLookups(CancellationToken ct)
    {
        var owners = await db.Owners.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive && x.Animals.Any(a => !a.IsDeleted && a.IsActive))
            .OrderBy(x => x.Name)
            .Select(x => new ClinicalLookupOwnerDto(
                x.Id,
                x.Code,
                x.Name,
                x.Animals
                    .Where(a => !a.IsDeleted && a.IsActive)
                    .OrderBy(a => a.Name)
                    .Select(a => new ClinicalLookupAnimalDto(a.Id, a.Code, a.Name))
                    .ToList()))
            .ToListAsync(ct);

        var doctors = await db.Doctors.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new ClinicalLookupDoctorDto(x.Id, x.Code, x.Name))
            .ToListAsync(ct);

        var medicines = await db.Medicines.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new ClinicalLookupMedicineDto(x.Id, x.Code, x.Name))
            .ToListAsync(ct);

        return Ok(new ClinicalLookupsDto(owners, doctors, medicines));
    }

    [HttpGet("appointments")]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> Appointments(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] AppointmentStatus? status,
        [FromQuery] AppointmentType? type,
        CancellationToken ct)
    {
        var query = db.Appointments.AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (from.HasValue) query = query.Where(x => x.ScheduledAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.ScheduledAt <= to.Value);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (type.HasValue) query = query.Where(x => x.Type == type.Value);

        return Ok(await query
            .OrderBy(x => x.ScheduledAt)
            .Select(x => new AppointmentDto(
                x.Id,
                x.OwnerId,
                x.Owner.Name,
                x.AnimalId,
                x.Animal.Name,
                x.DoctorId,
                x.Doctor != null ? x.Doctor.Name : null,
                x.Type,
                x.Status,
                x.ScheduledAt,
                x.Reason,
                x.QueueEntry != null ? x.QueueEntry.QueueNumber : null,
                x.QueueEntry != null ? x.QueueEntry.Status : null,
                x.QueueEntry != null ? x.QueueEntry.Priority : null,
                x.Encounter != null))
            .ToListAsync(ct));
    }

    [HttpPost("appointments"), Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<AppointmentDto>> Create(AppointmentCreateDto dto, CancellationToken ct)
    {
        var validation = await ValidateAppointmentAsync(dto, ct);
        if (validation is not null) return validation;

        var appointment = new Appointment
        {
            OwnerId = dto.OwnerId,
            AnimalId = dto.AnimalId,
            DoctorId = dto.DoctorId,
            Type = dto.Type,
            ScheduledAt = dto.ScheduledAt,
            Reason = Clean(dto.Reason),
            Status = dto.Type == AppointmentType.Scheduled ? AppointmentStatus.Scheduled : AppointmentStatus.CheckedIn
        };

        db.Appointments.Add(appointment);
        await db.SaveChangesAsync(ct);

        if (dto.Type != AppointmentType.Scheduled)
        {
            var priority = dto.Type == AppointmentType.Emergency ? 1 : dto.Priority;
            await CheckInAsync(appointment, priority, ct);
        }

        return CreatedAtAction(nameof(GetAppointment), new { appointmentId = appointment.Id }, await GetDtoAsync(appointment.Id, ct));
    }

    [HttpGet("appointments/{appointmentId:int}")]
    public async Task<ActionResult<AppointmentDto>> GetAppointment(int appointmentId, CancellationToken ct) =>
        (await GetDtoAsync(appointmentId, ct)) is { } item ? Ok(item) : NotFound();

    [HttpPost("appointments/{appointmentId:int}/check-in"), Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CheckIn(int appointmentId, [FromQuery] int? priority, CancellationToken ct)
    {
        var appointment = await db.Appointments.FirstOrDefaultAsync(x => x.Id == appointmentId && !x.IsDeleted, ct);
        if (appointment is null) return NotFound();

        await CheckInAsync(appointment, priority, ct);
        return NoContent();
    }

    [HttpGet("queue")]
    public async Task<ActionResult<IEnumerable<QueueDto>>> Queue(CancellationToken ct) =>
        Ok(await db.QueueEntries.AsNoTracking()
            .Include(x => x.Appointment).ThenInclude(x => x.Animal)
            .Include(x => x.Appointment).ThenInclude(x => x.Owner)
            .Include(x => x.Appointment).ThenInclude(x => x.Doctor)
            .Where(x => !x.IsDeleted && (x.Status == QueueStatus.Waiting || x.Status == QueueStatus.Called || x.Status == QueueStatus.InConsultation))
            .OrderBy(x => x.Priority ?? int.MaxValue)
            .ThenBy(x => x.QueuedAt)
            .Select(x => new QueueDto(
                x.Id,
                x.AppointmentId,
                x.QueueNumber,
                x.QueuedAt,
                x.Status,
                x.Priority,
                x.Appointment.Animal.Name,
                x.Appointment.Owner.Name,
                x.Appointment.Type,
                x.Appointment.DoctorId,
                x.Appointment.Doctor != null ? x.Appointment.Doctor.Name : null))
            .ToListAsync(ct));

    [HttpPost("queue/{queueId:int}/call"), Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Call(int queueId, CancellationToken ct)
    {
        var queue = await db.QueueEntries.Include(x => x.Appointment).FirstOrDefaultAsync(x => x.Id == queueId && !x.IsDeleted, ct);
        if (queue is null) return NotFound();

        queue.Status = QueueStatus.InConsultation;
        queue.Appointment.Status = AppointmentStatus.InConsultation;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("appointments/{appointmentId:int}/emr")]
    public async Task<ActionResult<EncounterDto>> GetEmr(int appointmentId, CancellationToken ct)
    {
        var encounter = await db.ClinicalEncounters.AsNoTracking()
            .Include(x => x.Prescriptions.Where(p => !p.IsDeleted))
            .Include(x => x.Vaccinations.Where(v => !v.IsDeleted))
            .FirstOrDefaultAsync(x => x.AppointmentId == appointmentId && !x.IsDeleted, ct);

        return encounter is null ? NotFound() : Ok(Map(encounter));
    }

    [HttpPut("appointments/{appointmentId:int}/emr"), Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<EncounterDto>> SaveEmr(int appointmentId, EncounterUpsertDto dto, CancellationToken ct)
    {
        var appointment = await db.Appointments.FirstOrDefaultAsync(x => x.Id == appointmentId && !x.IsDeleted, ct);
        if (appointment is null) return NotFound();

        var validation = await ValidateEncounterAsync(dto, ct);
        if (validation is not null) return validation;

        var encounter = await db.ClinicalEncounters
            .Include(x => x.Prescriptions)
            .Include(x => x.Vaccinations)
            .FirstOrDefaultAsync(x => x.AppointmentId == appointmentId && !x.IsDeleted, ct);

        if (encounter is null)
        {
            encounter = new ClinicalEncounter { AppointmentId = appointmentId };
            db.ClinicalEncounters.Add(encounter);
        }

        encounter.DoctorId = dto.DoctorId;
        encounter.ChiefComplaint = Clean(dto.ChiefComplaint);
        encounter.History = Clean(dto.History);
        encounter.Vitals = Clean(dto.Vitals);
        encounter.ClinicalExamination = Clean(dto.ClinicalExamination);
        encounter.Diagnosis = Clean(dto.Diagnosis);
        encounter.TreatmentPlan = Clean(dto.TreatmentPlan);
        encounter.FollowUpAt = dto.FollowUpAt;

        if (dto.Prescriptions is not null)
        {
            db.PrescriptionItems.RemoveRange(encounter.Prescriptions);
            encounter.Prescriptions = dto.Prescriptions
                .Where(x => !string.IsNullOrWhiteSpace(x.MedicineName))
                .Select(x => new PrescriptionItem
                {
                    MedicineId = x.MedicineId,
                    MedicineName = x.MedicineName.Trim(),
                    Dosage = Clean(x.Dosage),
                    Frequency = Clean(x.Frequency),
                    Duration = Clean(x.Duration),
                    Instructions = Clean(x.Instructions)
                })
                .ToList();
        }

        if (dto.Vaccinations is not null)
        {
            db.VaccinationRecords.RemoveRange(encounter.Vaccinations);
            encounter.Vaccinations = dto.Vaccinations
                .Where(x => !string.IsNullOrWhiteSpace(x.VaccineName))
                .Select(x => new VaccinationRecord
                {
                    VaccineName = x.VaccineName.Trim(),
                    AdministeredOn = x.AdministeredOn,
                    DueOn = x.DueOn,
                    BatchNumber = Clean(x.BatchNumber),
                    Notes = Clean(x.Notes)
                })
                .ToList();
        }

        appointment.Status = AppointmentStatus.Completed;
        var queue = await db.QueueEntries.FirstOrDefaultAsync(x => x.AppointmentId == appointmentId && !x.IsDeleted, ct);
        if (queue is not null) queue.Status = QueueStatus.Completed;

        await db.SaveChangesAsync(ct);
        return Ok(Map(encounter));
    }

    private async Task<ActionResult?> ValidateAppointmentAsync(AppointmentCreateDto dto, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

        var ownerExists = await db.Owners.AnyAsync(x => x.Id == dto.OwnerId && !x.IsDeleted, ct);
        var animalExists = await db.Animals.AnyAsync(x => x.Id == dto.AnimalId && x.OwnerId == dto.OwnerId && !x.IsDeleted, ct);

        if (!ownerExists) errors[nameof(dto.OwnerId)] = ["Owner does not exist."];
        if (!animalExists) errors[nameof(dto.AnimalId)] = ["Animal must exist and belong to the selected owner."];
        if (dto.DoctorId.HasValue && !await db.Doctors.AnyAsync(x => x.Id == dto.DoctorId && !x.IsDeleted, ct))
            errors[nameof(dto.DoctorId)] = ["Doctor does not exist."];
        if (dto.ScheduledAt == default)
            errors[nameof(dto.ScheduledAt)] = ["Scheduled date is required."];
        if (dto.Reason?.Trim().Length > 1000)
            errors[nameof(dto.Reason)] = ["Reason cannot exceed 1000 characters."];
        if (dto.Priority is < 1 or > 999)
            errors[nameof(dto.Priority)] = ["Priority must be between 1 and 999."];

        return errors.Count == 0
            ? null
            : new BadRequestObjectResult(new ValidationProblemDetails(errors));
    }

    private async Task<ActionResult?> ValidateEncounterAsync(EncounterUpsertDto dto, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

        if (dto.DoctorId.HasValue && !await db.Doctors.AnyAsync(x => x.Id == dto.DoctorId && !x.IsDeleted, ct))
            errors[nameof(dto.DoctorId)] = ["Doctor does not exist."];
        if (dto.FollowUpAt.HasValue && dto.FollowUpAt.Value < DateTime.UtcNow.AddYears(-5))
            errors[nameof(dto.FollowUpAt)] = ["Follow up date is invalid."];

        if (dto.Prescriptions is not null)
        {
            for (var index = 0; index < dto.Prescriptions.Count; index++)
            {
                var item = dto.Prescriptions[index];
                if (string.IsNullOrWhiteSpace(item.MedicineName)) continue;

                if (item.MedicineName.Trim().Length > 200)
                    errors[$"{nameof(dto.Prescriptions)}[{index}].{nameof(item.MedicineName)}"] = ["Medicine name cannot exceed 200 characters."];

                if (item.MedicineId.HasValue && !await db.Medicines.AnyAsync(x => x.Id == item.MedicineId && !x.IsDeleted, ct))
                    errors[$"{nameof(dto.Prescriptions)}[{index}].{nameof(item.MedicineId)}"] = ["Medicine does not exist."];
            }
        }

        if (dto.Vaccinations is not null)
        {
            for (var index = 0; index < dto.Vaccinations.Count; index++)
            {
                var item = dto.Vaccinations[index];
                if (string.IsNullOrWhiteSpace(item.VaccineName))
                    errors[$"{nameof(dto.Vaccinations)}[{index}].{nameof(item.VaccineName)}"] = ["Vaccine name is required."];
                else if (item.VaccineName.Trim().Length > 200)
                    errors[$"{nameof(dto.Vaccinations)}[{index}].{nameof(item.VaccineName)}"] = ["Vaccine name cannot exceed 200 characters."];

                if (item.DueOn.HasValue && item.DueOn.Value < item.AdministeredOn)
                    errors[$"{nameof(dto.Vaccinations)}[{index}].{nameof(item.DueOn)}"] = ["Due date cannot be before administered date."];
            }
        }

        return errors.Count == 0
            ? null
            : new BadRequestObjectResult(new ValidationProblemDetails(errors));
    }

    private async Task CheckInAsync(Appointment appointment, int? priority, CancellationToken ct)
    {
        if (!await db.QueueEntries.AnyAsync(x => x.AppointmentId == appointment.Id && !x.IsDeleted, ct))
        {
            var queueNumber = (await db.QueueEntries
                .Where(x => x.QueuedAt.Date == DateTime.UtcNow.Date)
                .MaxAsync(x => (int?)x.QueueNumber, ct) ?? 0) + 1;

            db.QueueEntries.Add(new QueueEntry
            {
                AppointmentId = appointment.Id,
                QueueNumber = queueNumber,
                Priority = priority
            });
        }

        appointment.Status = AppointmentStatus.CheckedIn;
        await db.SaveChangesAsync(ct);
    }

    private async Task<AppointmentDto?> GetDtoAsync(int id, CancellationToken ct) =>
        await db.Appointments.AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new AppointmentDto(
                x.Id,
                x.OwnerId,
                x.Owner.Name,
                x.AnimalId,
                x.Animal.Name,
                x.DoctorId,
                x.Doctor != null ? x.Doctor.Name : null,
                x.Type,
                x.Status,
                x.ScheduledAt,
                x.Reason,
                x.QueueEntry != null ? x.QueueEntry.QueueNumber : null,
                x.QueueEntry != null ? x.QueueEntry.Status : null,
                x.QueueEntry != null ? x.QueueEntry.Priority : null,
                x.Encounter != null))
            .FirstOrDefaultAsync(ct);

    private static EncounterDto Map(ClinicalEncounter encounter) => new(
        encounter.Id,
        encounter.AppointmentId,
        encounter.DoctorId,
        encounter.EncounteredAt,
        encounter.ChiefComplaint,
        encounter.History,
        encounter.Vitals,
        encounter.ClinicalExamination,
        encounter.Diagnosis,
        encounter.TreatmentPlan,
        encounter.FollowUpAt,
        encounter.Prescriptions.Select(p => new PrescriptionUpsertDto(p.MedicineId, p.MedicineName, p.Dosage, p.Frequency, p.Duration, p.Instructions)).ToList(),
        encounter.Vaccinations.Select(v => new VaccinationRecordDto(v.Id, v.VaccineName, v.AdministeredOn, v.DueOn, v.BatchNumber, v.Notes)).ToList());

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
