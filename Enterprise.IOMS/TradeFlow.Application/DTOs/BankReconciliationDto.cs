namespace TradeFlow.Application.DTOs;

public record BankStatementDto(Guid Id, string BankAccountName, DateTime StatementDate,
    string? FileName, DateTime ImportedAt, bool IsReconciled, List<BankStatementLineDto> Lines);
public record BankStatementLineDto(Guid Id, DateTime TransactionDate, string? Description,
    decimal Amount, string? Reference, Guid? MatchedPaymentId, bool IsMatched);
public record CreateBankStatementDto(string BankAccountName, DateTime StatementDate, string? FileName);
public record CreateBankStatementLineDto(DateTime TransactionDate, string? Description,
    decimal Amount, string? Reference);
