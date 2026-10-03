using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface IBranchService
{
    Task<List<BranchDto>> GetBranches();

    /// <summary>Active branches only, for the order filters that previously read the context directly.</summary>
    Task<List<BranchDto>> GetActiveBranchesAsync();

    Task<PagedResultNew<BranchDto>> GetBranchesPagedAsync(BranchPagedRequest request);
    Task<BranchDto?> GetBranchById(Guid id);
    Task<Guid> CreateBranch(CreateBranchDto dto);
    Task UpdateBranch(Guid id, CreateBranchDto dto);
    Task DeleteBranch(Guid id);
}
