using System;
using System.Collections.Generic;
using System.Text;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.DTOs
{
    // Sales Quotes
    public record SalesQuoteDto(Guid Id, string QuoteNumber, Guid CustomerId, string CustomerName,
        QuoteStatus Status, DateTime QuoteDate, DateTime ValidUntil, int Version,
        decimal SubTotal, decimal TaxAmount, decimal DiscountAmount, decimal TotalAmount,
        string? Notes, List<SalesQuoteItemDto> Items);

    public record SalesQuoteItemDto(Guid Id, Guid ProductId, string ProductName, string ProductSKU,
        int Quantity, decimal UnitPrice, decimal DiscountPercent, decimal TaxRate,
        decimal TaxAmount, decimal LineTotal);

    public record CreateSalesQuoteDto(Guid CustomerId, DateTime QuoteDate, DateTime ValidUntil,
        string? Notes, List<CreateSalesQuoteItemDto> Items, Guid? CurrencyId = null);

    public record CreateSalesQuoteItemDto(Guid ProductId, int Quantity, decimal UnitPrice,
        decimal DiscountPercent);

    public class CreateSalesQuotationDto
    {
        public Guid? CustomerId { get; set; }        
        public OrderStatus? Status { get; set; }
        public DateTime? QuatationDate { get; set; } = DateTime.UtcNow;
        public DateTime? ValidUntil { get; set; } = DateTime.UtcNow.AddDays(30);
        public decimal SubTotal { get; set; }        
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }
        public ControlDto CustomerDdlControl { get; set; } = new ControlDto();
        //public ControlDto SelectedCustomer { get; set; } = new FilterItem(Guid.Empty, "", false);
        public SalesQuotationItemDto ProductObj { get; set; } = new SalesQuotationItemDto();
        public List<CreateSalesQuoteItemDto> ItemsLine { get; set; } = new();

        public class SalesQuotationItemDto
        {
            public ControlDto ProductDdlControl { get; set; } = new ControlDto();
            public Guid ProductId { get; set; }
            public int Quantity { get; set; }
            public Guid Serial { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal DiscountAmount { get; set; }
            public DiscountType DiscountType { get; set; }
            public decimal TotalDiscount { get; set; }
            public decimal TotalPrice { get; set; }
            public List<Guid> SelectedSerialIds { get; set; } = new();
        }
    }

  //  public record CreateSalesQuotationItemDto(Guid ProductId, int Quantity, decimal UnitPrice, decimal DiscountAmount, DiscountType DiscountType, decimal TotalDiscountAmount, decimal TotalPrice, Guid? UoMId, List<Guid>? SerialIds = null);
}
