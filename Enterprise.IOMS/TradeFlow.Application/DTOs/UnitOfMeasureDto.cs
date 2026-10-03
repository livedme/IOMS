namespace TradeFlow.Application.DTOs;

public record UnitOfMeasureDto(Guid Id, string Name, string Abbreviation, bool IsBaseUnit);
public record UoMConversionDto(Guid Id, Guid FromUoMId, string FromUoMName,
    Guid ToUoMId, string ToUoMName, decimal ConversionFactor);
