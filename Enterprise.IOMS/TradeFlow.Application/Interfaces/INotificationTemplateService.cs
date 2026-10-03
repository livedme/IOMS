using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface INotificationTemplateService
{
    Task<List<NotificationTemplateDto>> GetTemplates();
    Task<Guid> CreateTemplate(CreateNotificationTemplateDto dto);
    Task UpdateTemplate(Guid id, CreateNotificationTemplateDto dto);
    Task DeleteTemplate(Guid id);

    /// <summary>Recent delivery attempts, for the settings screen. Was read straight off the context.</summary>
    Task<List<NotificationLogDto>> GetRecentLogs(int count);
}
