using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface ITaxService
{
    Task<List<TaxRateDto>> GetTaxRates();
    Task<Guid> CreateTaxRate(CreateTaxRateDto dto);
    Task UpdateTaxRate(Guid id, CreateTaxRateDto dto);
    Task<List<TaxJurisdictionDto>> GetJurisdictions();
    Task<Guid> CreateJurisdiction(CreateTaxJurisdictionDto dto);
    Task<decimal> CalculateTax(Guid jurisdictionId, decimal amount);
}
