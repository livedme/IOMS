using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.DTOs;

public record DataSubjectRequestDto(Guid Id, DataSubjectRequestType RequestType, string SubjectEmail,
    DataSubjectRequestStatus Status, DateTime? CompletedAt, string? Notes);
public record CreateDataSubjectRequestDto(DataSubjectRequestType RequestType, string SubjectEmail, string? Notes);
