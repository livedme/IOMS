using System;
using System.Collections.Generic;
using System.Text;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.DTOs
{

    // Payments
    public record PaymentDto(Guid Id, Guid InvoiceId, string? InvoiceNumber, decimal Amount,
        DateTime PaymentDate, PaymentMethod Method, string? Reference, string? Notes);

    public record CreatePaymentDto(Guid InvoiceId, decimal Amount, DateTime PaymentDate,
        PaymentMethod Method, string? Reference, string? Notes);

    public record RecordPaymentDto(PaymentType PaymentType, Guid? CustomerId, Guid? SupplierId,
        Guid? InvoiceId, decimal Amount, PaymentMethod PaymentMethod,
        string? Reference, DateTime PaymentDate);


}
