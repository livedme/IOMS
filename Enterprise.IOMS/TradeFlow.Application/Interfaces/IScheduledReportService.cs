using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface IScheduledReportService
{
    Task<List<ScheduledReportDto>> GetScheduledReports();
    Task<Guid> CreateScheduledReport(CreateScheduledReportDto dto);
    Task UpdateScheduledReport(Guid id, CreateScheduledReportDto dto);
    Task DeleteScheduledReport(Guid id);
}
