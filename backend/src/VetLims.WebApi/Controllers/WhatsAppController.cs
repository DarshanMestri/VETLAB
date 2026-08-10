using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;
using VetLims.WebApi.DTOs;
using VetLims.WebApi.Services;

namespace VetLims.WebApi.Controllers;

[ApiController]
[Route("api/whatsapp")]
[Authorize]
public sealed class WhatsAppController : ControllerBase
{
    private readonly VetLimsDbContext _db;
    private readonly WhatsAppMessagingService _service;

    public WhatsAppController(VetLimsDbContext db, WhatsAppMessagingService service)
    {
        _db = db;
        _service = service;
    }

    [HttpGet("templates")]
    public async Task<ActionResult<object>> Templates(
        [FromQuery] string? search,
        [FromQuery] WhatsAppMessageType? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.WhatsAppTemplates.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Code, term) ||
                EF.Functions.ILike(x.Name, term) ||
                EF.Functions.ILike(x.BodyTemplate, term));
        }
        if (type.HasValue)
            query = query.Where(x => x.MessageType == type.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.MessageType)
            .ThenBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new WhatsAppTemplateDto(x.Id, x.Code, x.Name, x.MessageType, x.LanguageCode, x.HeaderText, x.BodyTemplate, x.FooterText, x.IsActive))
            .ToListAsync(ct);

        return Ok(new { total, items, page, pageSize });
    }

    [HttpPost("templates"), Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<WhatsAppTemplateDto>> CreateTemplate(WhatsAppTemplateUpsertDto dto, CancellationToken ct)
    {
        var validation = await ValidateTemplateAsync(dto, null, ct);
        if (validation is not null) return validation;

        var entity = new WhatsAppTemplate();
        Apply(entity, dto);
        _db.WhatsAppTemplates.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Templates), new { id = entity.Id }, Map(entity));
    }

    [HttpPut("templates/{id:int}"), Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<WhatsAppTemplateDto>> UpdateTemplate(int id, WhatsAppTemplateUpsertDto dto, CancellationToken ct)
    {
        var entity = await _db.WhatsAppTemplates.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (entity is null) return NotFound();

        var validation = await ValidateTemplateAsync(dto, id, ct);
        if (validation is not null) return validation;

        Apply(entity, dto);
        await _db.SaveChangesAsync(ct);
        return Ok(Map(entity));
    }

    [HttpDelete("templates/{id:int}"), Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteTemplate(int id, CancellationToken ct)
    {
        var entity = await _db.WhatsAppTemplates.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (entity is null) return NotFound();

        _db.WhatsAppTemplates.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("queue")]
    public async Task<ActionResult<object>> Queue(
        [FromQuery] string? search,
        [FromQuery] WhatsAppMessageStatus? status,
        [FromQuery] WhatsAppMessageType? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.WhatsAppMessageQueue.AsNoTracking()
            .Include(x => x.Template)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.RecipientName, term) ||
                EF.Functions.ILike(x.ToNumber, term) ||
                (x.ExternalReference != null && EF.Functions.ILike(x.ExternalReference, term)) ||
                EF.Functions.ILike(x.MessageBody, term));
        }
        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);
        if (type.HasValue)
            query = query.Where(x => x.MessageType == type.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new WhatsAppQueueDto(
                x.Id,
                x.MessageType,
                x.Status,
                x.ToNumber,
                x.RecipientName,
                x.Template != null ? x.Template.Name : null,
                x.MessageBody,
                x.AttachmentFileName,
                x.ProviderMessageId,
                x.LastError,
                x.RetryCount,
                x.MaxRetryCount,
                x.NextAttemptAt,
                x.LastAttemptAt,
                x.SentAt,
                x.DeliveredAt,
                x.ExternalReference,
                x.CreatedAt))
            .ToListAsync(ct);

        return Ok(new { total, items, page, pageSize });
    }

    [HttpPost("queue/process-due"), Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<IReadOnlyList<WhatsAppQueueActionResultDto>>> ProcessDue([FromQuery] int limit = 20, CancellationToken ct = default) =>
        Ok(await _service.ProcessDueAsync(limit, ct));

    [HttpPost("queue/{id:int}/process"), Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<WhatsAppQueueActionResultDto>> Process(int id, CancellationToken ct = default)
    {
        try
        {
            return Ok(await _service.ProcessAsync(id, ct));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("queue/{id:int}/retry"), Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<WhatsAppQueueActionResultDto>> Retry(int id, CancellationToken ct = default)
    {
        try
        {
            return Ok(await _service.RetryAsync(id, ct));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("queue/{id:int}/delivery-status"), Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateDeliveryStatus(int id, WhatsAppDeliveryStatusUpdateDto dto, CancellationToken ct = default)
    {
        try
        {
            await _service.UpdateDeliveryStatusAsync(id, dto, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("send/report/{registrationItemId:int}")]
    public async Task<ActionResult<WhatsAppQueueActionResultDto>> SendReport(int registrationItemId, WhatsAppQueueRequestDto dto, CancellationToken ct = default)
    {
        try
        {
            return Ok(await _service.QueueReportAsync(registrationItemId, dto, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("send/invoice/{registrationId:int}")]
    public async Task<ActionResult<WhatsAppQueueActionResultDto>> SendInvoice(int registrationId, WhatsAppQueueRequestDto dto, CancellationToken ct = default)
    {
        try
        {
            return Ok(await _service.QueueInvoiceAsync(registrationId, dto, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("send/receipt/{registrationId:int}")]
    public async Task<ActionResult<WhatsAppQueueActionResultDto>> SendReceipt(int registrationId, WhatsAppQueueRequestDto dto, CancellationToken ct = default)
    {
        try
        {
            return Ok(await _service.QueueReceiptAsync(registrationId, dto, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("send/appointment-reminder/{appointmentId:int}")]
    public async Task<ActionResult<WhatsAppQueueActionResultDto>> SendAppointmentReminder(int appointmentId, WhatsAppQueueRequestDto dto, CancellationToken ct = default)
    {
        try
        {
            return Ok(await _service.QueueAppointmentReminderAsync(appointmentId, dto, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("send/vaccination-reminder/{vaccinationRecordId:int}")]
    public async Task<ActionResult<WhatsAppQueueActionResultDto>> SendVaccinationReminder(int vaccinationRecordId, WhatsAppQueueRequestDto dto, CancellationToken ct = default)
    {
        try
        {
            return Ok(await _service.QueueVaccinationReminderAsync(vaccinationRecordId, dto, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private async Task<ActionResult?> ValidateTemplateAsync(WhatsAppTemplateUpsertDto dto, int? currentId, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        var code = dto.Code.Trim();
        var name = dto.Name.Trim();
        var language = dto.LanguageCode.Trim();
        var body = dto.BodyTemplate.Trim();

        if (string.IsNullOrWhiteSpace(code)) errors[nameof(dto.Code)] = ["Code is required."];
        else if (code.Length > 50) errors[nameof(dto.Code)] = ["Code cannot exceed 50 characters."];
        if (string.IsNullOrWhiteSpace(name)) errors[nameof(dto.Name)] = ["Name is required."];
        else if (name.Length > 200) errors[nameof(dto.Name)] = ["Name cannot exceed 200 characters."];
        if (string.IsNullOrWhiteSpace(language)) errors[nameof(dto.LanguageCode)] = ["Language code is required."];
        else if (language.Length > 10) errors[nameof(dto.LanguageCode)] = ["Language code cannot exceed 10 characters."];
        if (string.IsNullOrWhiteSpace(body)) errors[nameof(dto.BodyTemplate)] = ["Body template is required."];
        else if (body.Length > 4000) errors[nameof(dto.BodyTemplate)] = ["Body template cannot exceed 4000 characters."];

        if (await _db.WhatsAppTemplates.AnyAsync(x => !x.IsDeleted && x.Id != currentId && x.Code == code, ct))
            errors[nameof(dto.Code)] = ["Template code already exists."];

        if (errors.Count > 0)
            return ValidationProblem(new ValidationProblemDetails(errors));

        return null;
    }

    private static void Apply(WhatsAppTemplate entity, WhatsAppTemplateUpsertDto dto)
    {
        entity.Code = dto.Code.Trim();
        entity.Name = dto.Name.Trim();
        entity.MessageType = dto.MessageType;
        entity.LanguageCode = dto.LanguageCode.Trim();
        entity.HeaderText = Clean(dto.HeaderText);
        entity.BodyTemplate = dto.BodyTemplate.Trim();
        entity.FooterText = Clean(dto.FooterText);
        entity.IsActive = dto.IsActive;
    }

    private static WhatsAppTemplateDto Map(WhatsAppTemplate entity) =>
        new(entity.Id, entity.Code, entity.Name, entity.MessageType, entity.LanguageCode, entity.HeaderText, entity.BodyTemplate, entity.FooterText, entity.IsActive);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
