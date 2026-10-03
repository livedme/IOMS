using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.DTOs;

public record NotificationLogDto(Guid Id, string UserId, string EventType, NotificationChannel Channel,
    string? Status, DateTime? SentAt);

// Notification Template
public record NotificationTemplateDto(Guid Id, string EventType, NotificationChannel Channel,
    string Subject, string BodyTemplate);
public record CreateNotificationTemplateDto(string EventType, NotificationChannel Channel,
    string Subject, string BodyTemplate);
