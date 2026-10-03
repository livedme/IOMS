using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface IDataPrivacyService
{
    Task<List<DataSubjectRequestDto>> GetRequests();
    Task<Guid> CreateRequest(CreateDataSubjectRequestDto dto);
    Task ProcessRequest(Guid id);
}
