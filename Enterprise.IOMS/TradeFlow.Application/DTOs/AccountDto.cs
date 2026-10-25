using System;
using System.Collections.Generic;
using System.Text;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.DTOs
{

    public class AccountModelDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public AccountType AccountType { get; set; }
        public Guid? ParentAccountId { get; set; }
        public string? Description { get; set; }

        //public AccountModelDto(string Code, string Name, AccountType AccountType, Guid? ParentAccountId, string? Description)
        //{
        //    this.Code = Code;
        //    this.Name = Name;
        //    this.AccountType = AccountType;
        //    this.ParentAccountId = ParentAccountId;
        //    this.Description = Description;
        //}
    }

    // Accounting
    public record AccountDto(Guid Id, string Code, string Name, AccountType AccountType,
        Guid? ParentAccountId, string? ParentAccountName, bool IsActive, bool IsSystemAccount,
        string? Description, decimal Balance);

    //public record CreateAccountDto(string Code, string Name, AccountType AccountType,
    //    Guid? ParentAccountId, string? Description);

    public record JournalEntryDto(Guid Id, string Reference, DateTime EntryDate, string? Description,
        bool IsAutoPosted, bool IsReversed, List<JournalEntryLineDto> Lines);

    public record JournalEntryLineDto(Guid Id, Guid AccountId, string AccountCode, string AccountName,
        decimal Debit, decimal Credit, string? Description);

    public record CreateJournalEntryDto(DateTime EntryDate, string? Description,
        List<CreateJournalEntryLineDto> Lines);

    public record CreateJournalEntryLineDto(Guid AccountId, decimal Debit, decimal Credit, string? Description);

    public class CreateJournalEntryDtoModels
    {
        public DateTime? EntryDate { get; set; } = DateTime.Today;
        public string? Description { get; set; }
        public JournalLineDtoModels LineObj { get; set; } = new JournalLineDtoModels();
        public List<CreateJournalEntryLineDto> JournalLines { get; set; } = new List<CreateJournalEntryLineDto>();

        public class JournalLineDtoModels
        {
            public ControlDto AccountDdlControl { get; set; } = new ControlDto();
            public Guid AccountId { get; set; }
            public string? Description { get; set; }
            public decimal Debit { get; set; }
            public decimal Credit { get; set; }
        }
    }
}
