namespace VetLims.Infrastructure.Entities;

public enum WhatsAppMessageType
{
    ReportPdf = 1,
    ReportJpg = 2,
    Invoice = 3,
    Receipt = 4,
    AppointmentReminder = 5,
    VaccinationReminder = 6
}

public enum WhatsAppMessageStatus
{
    Queued = 1,
    Processing = 2,
    Sent = 3,
    Delivered = 4,
    Failed = 5
}

public class WhatsAppTemplate : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public WhatsAppMessageType MessageType { get; set; }
    public string LanguageCode { get; set; } = "en";
    public string? HeaderText { get; set; }
    public string BodyTemplate { get; set; } = string.Empty;
    public string? FooterText { get; set; }
    public bool IsActive { get; set; } = true;
}

public class WhatsAppMessageQueue : BaseEntity
{
    public WhatsAppMessageType MessageType { get; set; }
    public WhatsAppMessageStatus Status { get; set; } = WhatsAppMessageStatus.Queued;
    public string ToNumber { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = "en";
    public int? TemplateId { get; set; }
    public WhatsAppTemplate? Template { get; set; }

    public int? OwnerId { get; set; }
    public Owner? Owner { get; set; }
    public int? RegistrationId { get; set; }
    public Registration? Registration { get; set; }
    public int? RegistrationItemId { get; set; }
    public RegistrationItem? RegistrationItem { get; set; }
    public int? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public int? VaccinationRecordId { get; set; }
    public VaccinationRecord? VaccinationRecord { get; set; }

    public string? ExternalReference { get; set; }
    public string? MessageHeader { get; set; }
    public string MessageBody { get; set; } = string.Empty;
    public string? MessageFooter { get; set; }
    public string? PayloadJson { get; set; }
    public string? AttachmentFileName { get; set; }
    public string? AttachmentContentType { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? LastError { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetryCount { get; set; } = 3;
    public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}
