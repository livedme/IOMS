using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface IKitService
{
    Task<List<KitDto>> GetKits();
    Task<PagedResultNew<KitDto>> GetKitsPagedAsync(KitPagedRequest request);
    Task<KitDto> GetKitById(Guid id);
    Task<Guid> CreateKit(CreateKitDto dto);
    Task AssembleKit(KitAssemblyDto dto);
    Task DisassembleKit(KitAssemblyDto dto);
}
