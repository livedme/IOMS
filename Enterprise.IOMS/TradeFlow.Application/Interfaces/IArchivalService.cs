using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface IArchivalService
{
    Task<List<ArchivalPolicyDto>> GetPolicies();
    Task<Guid> CreatePolicy(CreateArchivalPolicyDto dto);
    Task UpdatePolicy(Guid id, CreateArchivalPolicyDto dto);
    Task DeletePolicy(Guid id);
}
