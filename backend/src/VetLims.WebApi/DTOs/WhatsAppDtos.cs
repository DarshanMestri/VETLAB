using VetLims.Infrastructure.Entities;

namespace VetLims.WebApi.DTOs;

public record WhatsAppTemplateDto(
    int Id,
    string Code,
    string Name,
    WhatsAppMessageType MessageType,
    string LanguageCode,
    string? HeaderText,
    string BodyTemplate,
    string? FooterText,
    bool IsActive);

public record WhatsAppTemplateUpsertDto(
    string Code,
    string Name,
    WhatsAppMessageType MessageType,
    string LanguageCode,
    string? HeaderText,
    string BodyTemplate,
    string? FooterText,
    bool IsActive = true);

public record WhatsAppQueueDto(
    int Id,
    WhatsAppMessageType MessageType,
    WhatsAppMessageStatus Status,
    string ToNumber,
    string RecipientName,
    string? TemplateName,
    string MessageBody,
    string? AttachmentFileName,
    string? ProviderMessageId,
    string? LastError,
    int RetryCount,
    int MaxRetryCount,
    DateTime NextAttemptAt,
    DateTime? LastAttemptAt,
    DateTime? SentAt,
    DateTime? DeliveredAt,
    string? ExternalReference,
    DateTime CreatedAt);

public record WhatsAppQueueRequestDto(
    int? TemplateId = null,
    string? PhoneNumber = null,
    string? Format = null,
    int? MaxRetryCount = null);

public record WhatsAppDeliveryStatusUpdateDto(
    WhatsAppMessageStatus Status,
    string? ProviderMessageId = null,
    string? ErrorMessage = null);

public record WhatsAppQueueActionResultDto(int QueueId, WhatsAppMessageStatus Status, string Message);
