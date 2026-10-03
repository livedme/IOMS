using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface IExchangeRateService
{
    Task<List<ExchangeRateDto>> GetExchangeRates();
    Task<Guid> CreateExchangeRate(Guid fromCurrencyId, Guid toCurrencyId, decimal rate, DateTime effectiveDate);
    Task DeleteExchangeRate(Guid id);
}
