using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.DTOs;

public record CustomFieldDefinitionDto(Guid Id, string EntityType, string FieldName,
    CustomFieldType FieldType, bool IsRequired, string? Options, int SortOrder);
public record CreateCustomFieldDefinitionDto(string EntityType, string FieldName,
    CustomFieldType FieldType, bool IsRequired, string? Options);
public record CustomFieldValueDto(Guid Id, Guid DefinitionId, string FieldName, string? Value);
