using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface ICustomFieldService
{
    Task<List<CustomFieldDefinitionDto>> GetDefinitions(string? entityType);
    Task<Guid> CreateDefinition(CreateCustomFieldDefinitionDto dto);
    Task DeleteDefinition(Guid id);
    Task<List<CustomFieldValueDto>> GetValues(Guid entityId);
    Task SetValue(Guid definitionId, Guid entityId, string? value);
}
