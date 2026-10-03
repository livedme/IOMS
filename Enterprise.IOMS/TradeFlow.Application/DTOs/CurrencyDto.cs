namespace TradeFlow.Application.DTOs;

public record CurrencyDto(Guid Id, string Code, string Name, string Symbol,
    int DecimalPlaces, bool IsBaseCurrency, bool IsActive);

public record ExchangeRateDto(Guid Id, Guid FromCurrencyId, string FromCurrencyCode,
    Guid ToCurrencyId, string ToCurrencyCode, decimal Rate, DateTime EffectiveDate);

// Currency Creation
public record CreateCurrencyDto(string Code, string Name, string Symbol, bool IsBaseCurrency = false);

// Exchange Rate Creation
public record CreateExchangeRateDto(Guid FromCurrencyId, Guid ToCurrencyId, decimal Rate, DateTime EffectiveDate);
