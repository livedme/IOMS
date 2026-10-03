using System;
using System.Collections.Generic;
using System.Text;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.DTOs
{

    // Accounting
    public record AccountDto(Guid Id, string Code, string Name, AccountType AccountType,
        Guid? ParentAccountId, string? ParentAccountName, bool IsActive, bool IsSystemAccount,
        string? Description, decimal Balance);

    public record CreateAccountDto(string Code, string Name, AccountType AccountType,
        Guid? ParentAccountId, string? Description);

    public record JournalEntryDto(Guid Id, string Reference, DateTime EntryDate, string? Description,
        bool IsAutoPosted, bool IsReversed, List<JournalEntryLineDto> Lines);

    public record JournalEntryLineDto(Guid Id, Guid AccountId, string AccountCode, string AccountName,
        decimal Debit, decimal Credit, string? Description);

    public record CreateJournalEntryDto(DateTime EntryDate, string? Description,
        List<CreateJournalEntryLineDto> Lines);

    public record CreateJournalEntryLineDto(Guid AccountId, decimal Debit, decimal Credit, string? Description);
}
