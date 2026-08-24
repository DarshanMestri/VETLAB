using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;
using VetLims.WebApi.DTOs;
using VetLims.WebApi.Reporting;

namespace VetLims.WebApi.Services;

public interface IWhatsAppProvider
{
    Task<WhatsAppDispatchResult> SendAsync(WhatsAppDispatchRequest request, CancellationToken cancellationToken = default);
}

public sealed record WhatsAppDispatchRequest(
    string ToNumber,
    string RecipientName,
    string MessageBody,
    string? MessageHeader,
    string? MessageFooter,
    string? AttachmentFileName,
    string? AttachmentContentType,
    byte[]? AttachmentContent);

public sealed record WhatsAppDispatchResult(bool Success, string? ProviderMessageId, string? ErrorMessage);

public sealed class WhatsAppProvider : IWhatsAppProvider
{
    private readonly IConfiguration _configuration;

    public WhatsAppProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<WhatsAppDispatchResult> SendAsync(WhatsAppDispatchRequest request, CancellationToken cancellationToken = default)
    {
        var section = _configuration.GetSection("WhatsAppBusinessApi");
        var enabled = section.GetValue<bool>("Enabled");
        var accessToken = section["AccessToken"];
        var phoneNumberId = section["PhoneNumberId"];

        if (!enabled || string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(phoneNumberId))
        {
            return Task.FromResult(new WhatsAppDispatchResult(
                false,
                null,
                "WhatsApp Business API is not configured. Set WhatsAppBusinessApi:Enabled, AccessToken, and PhoneNumberId to process queued messages."));
        }

        return Task.FromResult(new WhatsAppDispatchResult(
            false,
            null,
            "WhatsApp provider configuration exists, but the provider adapter is intentionally left as a ready structure for real Business API wiring."));
    }
}

public sealed class WhatsAppMessagingService
{
    private readonly VetLimsDbContext _db;
    private readonly ResultReportService _reports;
    private readonly IWhatsAppProvider _provider;

    public WhatsAppMessagingService(VetLimsDbContext db, ResultReportService reports, IWhatsAppProvider provider)
    {
        _db = db;
        _reports = reports;
        _provider = provider;
    }

    public Task<WhatsAppQueueActionResultDto> QueueReportAsync(int registrationItemId, WhatsAppQueueRequestDto dto, CancellationToken ct = default) =>
        EnqueueAsync(WhatsAppMessageTypeFromFormat(dto.Format, WhatsAppMessageType.ReportPdf), dto, registrationItemId: registrationItemId, cancellationToken: ct);

    public Task<WhatsAppQueueActionResultDto> QueueInvoiceAsync(int registrationId, WhatsAppQueueRequestDto dto, CancellationToken ct = default) =>
        EnqueueAsync(WhatsAppMessageType.Invoice, dto, registrationId: registrationId, cancellationToken: ct);

    public Task<WhatsAppQueueActionResultDto> QueueReceiptAsync(int registrationId, WhatsAppQueueRequestDto dto, CancellationToken ct = default) =>
        EnqueueAsync(WhatsAppMessageType.Receipt, dto, registrationId: registrationId, cancellationToken: ct);

    public Task<WhatsAppQueueActionResultDto> QueueAppointmentReminderAsync(int appointmentId, WhatsAppQueueRequestDto dto, CancellationToken ct = default) =>
        EnqueueAsync(WhatsAppMessageType.AppointmentReminder, dto, appointmentId: appointmentId, cancellationToken: ct);

    public Task<WhatsAppQueueActionResultDto> QueueVaccinationReminderAsync(int vaccinationRecordId, WhatsAppQueueRequestDto dto, CancellationToken ct = default) =>
        EnqueueAsync(WhatsAppMessageType.VaccinationReminder, dto, vaccinationRecordId: vaccinationRecordId, cancellationToken: ct);

    public async Task<IReadOnlyList<WhatsAppQueueActionResultDto>> ProcessDueAsync(int limit, CancellationToken ct = default)
    {
        var queueIds = await _db.WhatsAppMessageQueue
            .Where(x => !x.IsDeleted &&
                        (x.Status == WhatsAppMessageStatus.Queued || x.Status == WhatsAppMessageStatus.Failed) &&
                        x.NextAttemptAt <= DateTime.UtcNow &&
                        x.RetryCount < x.MaxRetryCount)
            .OrderBy(x => x.NextAttemptAt)
            .ThenBy(x => x.Id)
            .Select(x => x.Id)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(ct);

        var results = new List<WhatsAppQueueActionResultDto>(queueIds.Count);
        foreach (var queueId in queueIds)
            results.Add(await ProcessAsync(queueId, ct));

        return results;
    }

    public async Task<WhatsAppQueueActionResultDto> RetryAsync(int queueId, CancellationToken ct = default)
    {
        var queue = await _db.WhatsAppMessageQueue.FirstOrDefaultAsync(x => x.Id == queueId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Queue item was not found.");

        queue.Status = WhatsAppMessageStatus.Queued;
        queue.LastError = null;
        queue.NextAttemptAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new WhatsAppQueueActionResultDto(queue.Id, queue.Status, "Queue item scheduled for retry.");
    }

    public async Task<WhatsAppQueueActionResultDto> ProcessAsync(int queueId, CancellationToken ct = default)
    {
        var queue = await _db.WhatsAppMessageQueue
            .Include(x => x.Template)
            .FirstOrDefaultAsync(x => x.Id == queueId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Queue item was not found.");

        queue.Status = WhatsAppMessageStatus.Processing;
        queue.LastAttemptAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        WhatsAppDispatchResult dispatchResult;
        try
        {
            var dispatchRequest = await BuildDispatchRequestAsync(queue, ct);
            dispatchResult = await _provider.SendAsync(dispatchRequest, ct);
        }
        catch (InvalidOperationException ex)
        {
            dispatchResult = new WhatsAppDispatchResult(false, null, ex.Message);
        }

        if (dispatchResult.Success)
        {
            queue.Status = WhatsAppMessageStatus.Sent;
            queue.ProviderMessageId = dispatchResult.ProviderMessageId;
            queue.SentAt = DateTime.UtcNow;
            queue.LastError = null;
            await _db.SaveChangesAsync(ct);
            return new WhatsAppQueueActionResultDto(queue.Id, queue.Status, "Message sent to provider.");
        }

        queue.Status = WhatsAppMessageStatus.Failed;
        queue.RetryCount += 1;
        queue.LastError = dispatchResult.ErrorMessage;
        queue.NextAttemptAt = DateTime.UtcNow.AddMinutes(GetRetryDelayMinutes(queue.RetryCount));
        await _db.SaveChangesAsync(ct);

        return new WhatsAppQueueActionResultDto(queue.Id, queue.Status, queue.LastError ?? "Message dispatch failed.");
    }

    public async Task UpdateDeliveryStatusAsync(int queueId, WhatsAppDeliveryStatusUpdateDto dto, CancellationToken ct = default)
    {
        var queue = await _db.WhatsAppMessageQueue.FirstOrDefaultAsync(x => x.Id == queueId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Queue item was not found.");

        queue.Status = dto.Status;
        queue.ProviderMessageId = string.IsNullOrWhiteSpace(dto.ProviderMessageId) ? queue.ProviderMessageId : dto.ProviderMessageId.Trim();
        queue.LastError = string.IsNullOrWhiteSpace(dto.ErrorMessage) ? null : dto.ErrorMessage.Trim();

        if (dto.Status == WhatsAppMessageStatus.Sent && queue.SentAt is null)
            queue.SentAt = DateTime.UtcNow;
        if (dto.Status == WhatsAppMessageStatus.Delivered)
            queue.DeliveredAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    private async Task<WhatsAppQueueActionResultDto> EnqueueAsync(
        WhatsAppMessageType messageType,
        WhatsAppQueueRequestDto dto,
        int? registrationId = null,
        int? registrationItemId = null,
        int? appointmentId = null,
        int? vaccinationRecordId = null,
        CancellationToken cancellationToken = default)
    {
        var template = await ResolveTemplateAsync(messageType, dto.TemplateId, cancellationToken);
        var queue = messageType switch
        {
            WhatsAppMessageType.ReportPdf or WhatsAppMessageType.ReportJpg =>
                await BuildReportQueueAsync(messageType, registrationItemId ?? throw new InvalidOperationException("Registration item is required."), dto, template, cancellationToken),
            WhatsAppMessageType.Invoice or WhatsAppMessageType.Receipt =>
                await BuildRegistrationQueueAsync(messageType, registrationId ?? throw new InvalidOperationException("Registration is required."), dto, template, cancellationToken),
            WhatsAppMessageType.AppointmentReminder =>
                await BuildAppointmentQueueAsync(appointmentId ?? throw new InvalidOperationException("Appointment is required."), dto, template, cancellationToken),
            WhatsAppMessageType.VaccinationReminder =>
                await BuildVaccinationQueueAsync(vaccinationRecordId ?? throw new InvalidOperationException("Vaccination record is required."), dto, template, cancellationToken),
            _ => throw new InvalidOperationException("Unsupported WhatsApp message type.")
        };

        _db.WhatsAppMessageQueue.Add(queue);
        await _db.SaveChangesAsync(cancellationToken);
        return new WhatsAppQueueActionResultDto(queue.Id, queue.Status, "Message queued successfully.");
    }

    private async Task<WhatsAppMessageQueue> BuildReportQueueAsync(WhatsAppMessageType messageType, int registrationItemId, WhatsAppQueueRequestDto dto, WhatsAppTemplate? template, CancellationToken ct)
    {
        var item = await _db.RegistrationItems
            .AsNoTracking()
            .Include(x => x.Registration)
                .ThenInclude(x => x.Owner)
            .Include(x => x.Registration)
                .ThenInclude(x => x.Animals)
            .FirstOrDefaultAsync(x => x.Id == registrationItemId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Result order item was not found.");

        var phone = ResolvePhoneNumber(item.Registration.Owner, dto.PhoneNumber);
        var animalName = item.Registration.Animals.FirstOrDefault()?.AnimalName ?? "animal";
        var format = messageType == WhatsAppMessageType.ReportJpg ? "jpg" : "pdf";
        var placeholders = new Dictionary<string, string?>
        {
            ["ownerName"] = item.Registration.Owner?.Name ?? item.Registration.OwnerName,
            ["animalName"] = animalName,
            ["registrationBarcode"] = item.Registration.Barcode,
            ["itemBarcode"] = item.ItemBarcode,
            ["reportFormat"] = format.ToUpperInvariant()
        };

        return CreateQueueEntity(
            messageType,
            phone,
            placeholders["ownerName"] ?? item.Registration.OwnerName,
            item.Registration.OwnerId,
            template,
            placeholders,
            BuildDefaultBody(messageType, placeholders),
            JsonSerializer.Serialize(new { registrationItemId, format }),
            messageType == WhatsAppMessageType.ReportPdf ? $"result-{item.ItemBarcode}.pdf" : $"result-{item.ItemBarcode}.jpg",
            messageType == WhatsAppMessageType.ReportPdf ? "application/pdf" : "image/jpeg",
            item.RegistrationId,
            registrationItemId,
            externalReference: item.ItemBarcode,
            maxRetryCount: dto.MaxRetryCount);
    }

    private async Task<WhatsAppMessageQueue> BuildRegistrationQueueAsync(WhatsAppMessageType messageType, int registrationId, WhatsAppQueueRequestDto dto, WhatsAppTemplate? template, CancellationToken ct)
    {
        var registration = await _db.Registrations
            .AsNoTracking()
            .Include(x => x.Owner)
            .FirstOrDefaultAsync(x => x.Id == registrationId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Registration was not found.");

        var phone = ResolvePhoneNumber(registration.Owner, dto.PhoneNumber);
        var placeholders = new Dictionary<string, string?>
        {
            ["ownerName"] = registration.Owner?.Name ?? registration.OwnerName,
            ["registrationBarcode"] = registration.Barcode,
            ["referenceNumber"] = registration.ReferenceNumber,
            ["totalAmount"] = registration.TotalAmount.ToString("0.00"),
            ["paidAmount"] = registration.PaidAmount.ToString("0.00"),
            ["balanceAmount"] = registration.Balance.ToString("0.00")
        };

        return CreateQueueEntity(
            messageType,
            phone,
            placeholders["ownerName"] ?? registration.OwnerName,
            registration.OwnerId,
            template,
            placeholders,
            BuildDefaultBody(messageType, placeholders),
            JsonSerializer.Serialize(new { registrationId }),
            null,
            null,
            registrationId,
            null,
            externalReference: registration.Barcode,
            maxRetryCount: dto.MaxRetryCount);
    }

    private async Task<WhatsAppMessageQueue> BuildAppointmentQueueAsync(int appointmentId, WhatsAppQueueRequestDto dto, WhatsAppTemplate? template, CancellationToken ct)
    {
        var appointment = await _db.Appointments
            .AsNoTracking()
            .Include(x => x.Owner)
            .Include(x => x.Animal)
            .Include(x => x.Doctor)
            .FirstOrDefaultAsync(x => x.Id == appointmentId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Appointment was not found.");

        var phone = ResolvePhoneNumber(appointment.Owner, dto.PhoneNumber);
        var placeholders = new Dictionary<string, string?>
        {
            ["ownerName"] = appointment.Owner.Name,
            ["animalName"] = appointment.Animal.Name,
            ["doctorName"] = appointment.Doctor?.Name,
            ["scheduledAt"] = appointment.ScheduledAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
            ["reason"] = appointment.Reason
        };

        return CreateQueueEntity(
            WhatsAppMessageType.AppointmentReminder,
            phone,
            appointment.Owner.Name,
            appointment.OwnerId,
            template,
            placeholders,
            BuildDefaultBody(WhatsAppMessageType.AppointmentReminder, placeholders),
            JsonSerializer.Serialize(new { appointmentId }),
            null,
            null,
            null,
            null,
            appointmentId,
            externalReference: appointment.Id.ToString(),
            maxRetryCount: dto.MaxRetryCount);
    }

    private async Task<WhatsAppMessageQueue> BuildVaccinationQueueAsync(int vaccinationRecordId, WhatsAppQueueRequestDto dto, WhatsAppTemplate? template, CancellationToken ct)
    {
        var vaccination = await _db.VaccinationRecords
            .AsNoTracking()
            .Include(x => x.ClinicalEncounter)
                .ThenInclude(x => x.Appointment)
                    .ThenInclude(x => x.Owner)
            .Include(x => x.ClinicalEncounter)
                .ThenInclude(x => x.Appointment)
                    .ThenInclude(x => x.Animal)
            .FirstOrDefaultAsync(x => x.Id == vaccinationRecordId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Vaccination record was not found.");

        var appointment = vaccination.ClinicalEncounter.Appointment;
        var phone = ResolvePhoneNumber(appointment.Owner, dto.PhoneNumber);
        var placeholders = new Dictionary<string, string?>
        {
            ["ownerName"] = appointment.Owner.Name,
            ["animalName"] = appointment.Animal.Name,
            ["vaccineName"] = vaccination.VaccineName,
            ["administeredOn"] = vaccination.AdministeredOn.ToString("yyyy-MM-dd"),
            ["dueOn"] = vaccination.DueOn?.ToString("yyyy-MM-dd"),
            ["batchNumber"] = vaccination.BatchNumber
        };

        return CreateQueueEntity(
            WhatsAppMessageType.VaccinationReminder,
            phone,
            appointment.Owner.Name,
            appointment.OwnerId,
            template,
            placeholders,
            BuildDefaultBody(WhatsAppMessageType.VaccinationReminder, placeholders),
            JsonSerializer.Serialize(new { vaccinationRecordId }),
            null,
            null,
            null,
            null,
            appointment.Id,
            vaccinationRecordId,
            externalReference: vaccination.Id.ToString(),
            maxRetryCount: dto.MaxRetryCount);
    }

    private async Task<WhatsAppDispatchRequest> BuildDispatchRequestAsync(WhatsAppMessageQueue queue, CancellationToken ct)
    {
        byte[]? attachmentContent = null;

        if (queue.MessageType == WhatsAppMessageType.ReportPdf || queue.MessageType == WhatsAppMessageType.ReportJpg)
        {
            var payload = JsonSerializer.Deserialize<ReportPayload>(queue.PayloadJson ?? string.Empty)
                ?? throw new InvalidOperationException("Queued report payload is invalid.");

            var report = await _reports.GenerateAsync(payload.RegistrationItemId, payload.Format, null, false, ct)
                ?? throw new InvalidOperationException("Queued report could not be generated.");

            attachmentContent = report.Content;
        }

        return new WhatsAppDispatchRequest(
            queue.ToNumber,
            queue.RecipientName,
            queue.MessageBody,
            queue.MessageHeader,
            queue.MessageFooter,
            queue.AttachmentFileName,
            queue.AttachmentContentType,
            attachmentContent);
    }

    private async Task<WhatsAppTemplate?> ResolveTemplateAsync(WhatsAppMessageType messageType, int? templateId, CancellationToken ct)
    {
        if (templateId.HasValue)
        {
            return await _db.WhatsAppTemplates.FirstOrDefaultAsync(x => x.Id == templateId.Value && !x.IsDeleted && x.IsActive, ct)
                ?? throw new InvalidOperationException("WhatsApp template was not found.");
        }

        return await _db.WhatsAppTemplates
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive && x.MessageType == messageType)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }

    private static WhatsAppMessageQueue CreateQueueEntity(
        WhatsAppMessageType messageType,
        string toNumber,
        string recipientName,
        int? ownerId,
        WhatsAppTemplate? template,
        IReadOnlyDictionary<string, string?> placeholders,
        string defaultBody,
        string payloadJson,
        string? attachmentFileName,
        string? attachmentContentType,
        int? registrationId = null,
        int? registrationItemId = null,
        int? appointmentId = null,
        int? vaccinationRecordId = null,
        string? externalReference = null,
        int? maxRetryCount = null)
    {
        return new WhatsAppMessageQueue
        {
            MessageType = messageType,
            Status = WhatsAppMessageStatus.Queued,
            ToNumber = toNumber,
            RecipientName = recipientName,
            LanguageCode = template?.LanguageCode ?? "en",
            TemplateId = template?.Id,
            OwnerId = ownerId,
            RegistrationId = registrationId,
            RegistrationItemId = registrationItemId,
            AppointmentId = appointmentId,
            VaccinationRecordId = vaccinationRecordId,
            ExternalReference = externalReference,
            MessageHeader = RenderTemplate(template?.HeaderText, placeholders),
            MessageBody = RenderTemplate(template?.BodyTemplate, placeholders) ?? defaultBody,
            MessageFooter = RenderTemplate(template?.FooterText, placeholders),
            PayloadJson = payloadJson,
            AttachmentFileName = attachmentFileName,
            AttachmentContentType = attachmentContentType,
            MaxRetryCount = maxRetryCount is > 0 and <= 10 ? maxRetryCount.Value : 3,
            NextAttemptAt = DateTime.UtcNow
        };
    }

    private static string BuildDefaultBody(WhatsAppMessageType messageType, IReadOnlyDictionary<string, string?> placeholders)
    {
        return messageType switch
        {
            WhatsAppMessageType.ReportPdf or WhatsAppMessageType.ReportJpg =>
                $"Dear {placeholders["ownerName"]}, your {placeholders["reportFormat"]} report for {placeholders["animalName"]} is ready. Registration: {placeholders["registrationBarcode"]}, Sample: {placeholders["itemBarcode"]}.",
            WhatsAppMessageType.Invoice =>
                $"Dear {placeholders["ownerName"]}, invoice for registration {placeholders["registrationBarcode"]}: total {placeholders["totalAmount"]}, paid {placeholders["paidAmount"]}, balance {placeholders["balanceAmount"]}.",
            WhatsAppMessageType.Receipt =>
                $"Dear {placeholders["ownerName"]}, receipt for registration {placeholders["registrationBarcode"]}: received {placeholders["paidAmount"]}, balance {placeholders["balanceAmount"]}.",
            WhatsAppMessageType.AppointmentReminder =>
                $"Reminder: {placeholders["animalName"]} has an appointment on {placeholders["scheduledAt"]}{(string.IsNullOrWhiteSpace(placeholders["doctorName"]) ? string.Empty : $" with {placeholders["doctorName"]}")}.",
            WhatsAppMessageType.VaccinationReminder =>
                $"Reminder: {placeholders["animalName"]} is due for {placeholders["vaccineName"]} vaccination on {placeholders["dueOn"] ?? placeholders["administeredOn"]}.",
            _ => string.Empty
        };
    }

    private static string? RenderTemplate(string? template, IReadOnlyDictionary<string, string?> placeholders)
    {
        if (string.IsNullOrWhiteSpace(template))
            return null;

        var value = template;
        foreach (var (key, placeholderValue) in placeholders)
            value = value.Replace($"{{{{{key}}}}}", placeholderValue ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        return value;
    }

    private static string ResolvePhoneNumber(Owner? owner, string? overridePhoneNumber)
    {
        var phone = NormalizePhoneNumber(overridePhoneNumber)
            ?? NormalizePhoneNumber(owner?.WhatsApp)
            ?? NormalizePhoneNumber(owner?.Mobile);

        if (string.IsNullOrWhiteSpace(phone))
            throw new InvalidOperationException("A valid WhatsApp or mobile number is required.");

        return phone;
    }

    private static string? NormalizePhoneNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        var hasPlus = trimmed.StartsWith('+');
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length < 8)
            return null;

        return hasPlus ? $"+{digits}" : digits;
    }

    private static int GetRetryDelayMinutes(int retryCount) => retryCount switch
    {
        <= 1 => 5,
        2 => 15,
        3 => 30,
        _ => 60
    };

    private static WhatsAppMessageType WhatsAppMessageTypeFromFormat(string? format, WhatsAppMessageType defaultType)
    {
        return format?.Trim().ToLowerInvariant() switch
        {
            null or "" or "pdf" => WhatsAppMessageType.ReportPdf,
            "jpg" or "jpeg" => WhatsAppMessageType.ReportJpg,
            _ => throw new InvalidOperationException("Supported report formats for WhatsApp are pdf and jpg.")
        };
    }

    private sealed record ReportPayload(int RegistrationItemId, string Format);
}
