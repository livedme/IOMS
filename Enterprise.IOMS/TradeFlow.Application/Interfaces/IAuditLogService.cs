using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface IAuditLogService
{
    Task<PagedResult<AuditLogDto>> GetAuditLogs(string? tableName, string? action, int page, int pageSize);
}
