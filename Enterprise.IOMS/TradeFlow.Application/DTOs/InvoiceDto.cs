using System;
using System.Collections.Generic;
using System.Text;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.DTOs
{

    // Invoices
    public record InvoiceDto(Guid Id, string InvoiceNumber, InvoiceType InvoiceType, InvoiceStatus Status,
        Guid? CustomerId, string? CustomerName, Guid? SupplierId, string? SupplierName,
        DateTime InvoiceDate, DateTime DueDate, decimal SubTotal, decimal TaxAmount,
        decimal TotalAmount, decimal PaidAmount, decimal BalanceDue);

    public record CreateInvoiceDto(InvoiceType InvoiceType, Guid? SalesOrderId, Guid? PurchaseOrderId,
        Guid? CustomerId, Guid? SupplierId, DateTime DueDate, string? Notes);

    public record CreditNoteDto(Guid Id, string CreditNoteNumber, Guid InvoiceId, string InvoiceNumber,
        DateTime Date, decimal Amount, string? Reason, CreditNoteStatus Status);

    public record DebitNoteDto(Guid Id, string DebitNoteNumber, Guid InvoiceId, string InvoiceNumber,
        DateTime Date, decimal Amount, string? Reason, DebitNoteStatus Status);

    // Credit/Debit Note Creation
    public record CreateCreditNoteDto(Guid InvoiceId, decimal Amount, string? Reason);
    public record CreateDebitNoteDto(Guid InvoiceId, decimal Amount, string? Reason);

}
