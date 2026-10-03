using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface IUoMService
{
    Task<List<UnitOfMeasureDto>> GetUnitsOfMeasure();
    Task<Guid> CreateUnitOfMeasure(string name, string abbreviation, bool isBaseUnit);
    Task DeleteUnitOfMeasure(Guid id);
    Task<List<UoMConversionDto>> GetConversions();
    Task<Guid> CreateConversion(Guid fromUoMId, Guid toUoMId, decimal conversionFactor);
    Task DeleteConversion(Guid id);
}
