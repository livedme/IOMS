using System;
using System.Collections.Generic;
using System.Text;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.DTOs
{

    // Payments
    public class PaymentModelDto
    {
        public Guid Id { get; set; }        
        public Guid? InvoiceId { get; set; }
        public PaymentType PaymentType { get; set; }
        public DateTime? PaymentDate { get; set; }
        public ControlItemsDto? SelectedParty { get; set; }
        public PaymentMethod Method { get; set; }
        public string? PaymentMediaName { get; set; } // for credit card, check and bank transfer
        public string? AccountName { get; set; }
        public string? AccountNumber { get; set; }        
        public string? CheckOrCardNumber { get; set; }
        public string? Reference { get; set; }        
        public decimal Amount { get; set; }        
        public string? Notes { get; set; }
    }

    public record PaymentDto(Guid Id, Guid InvoiceId, string? InvoiceNumber, decimal Amount,
        DateTime PaymentDate, PaymentMethod Method, string? Reference, string? Notes);

    public record CreatePaymentDto(Guid InvoiceId, decimal Amount, DateTime PaymentDate,
        PaymentMethod Method, string? Reference, string? Notes);

    public record RecordPaymentDto(PaymentType PaymentType, Guid? CustomerId, Guid? SupplierId,
        Guid? InvoiceId, decimal Amount, PaymentMethod PaymentMethod, string PaymentMediaName,
        string AccountName, string AccountNumber, string CheckOrCardNumber,
        string? Reference, DateTime PaymentDate, string Notes);


    //PaymentMethod = dto.PaymentMediaName,
    //    PaymentMethod = dto.AccountName,
    //    PaymentMethod = dto.AccountNumber,
    //    PaymentMethod = dto.CheckOrCardNumber,
    //    Reference = dto.Reference,
    //    Reference = dto.Notes



}
