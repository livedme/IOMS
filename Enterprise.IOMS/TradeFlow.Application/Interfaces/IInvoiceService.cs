using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Interfaces;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceDto>> GetInvoicesAsync(string? search, InvoiceStatus? status, InvoiceType? type, int page, int pageSize);
    Task<InvoiceDto?> GetInvoiceByIdAsync(Guid id);

    /// <summary>Full invoice detail for the view panel: source order, line items and party contact info.</summary>
    Task<InvoiceDetailDto?> GetInvoiceDetailAsync(Guid id);
    Task<List<InvoiceDto?>> GetInvoiceBySupplier(Guid SupplierId);
    Task<List<InvoiceDto?>> GetInvoiceByCustomer(Guid CustomerId);
    Task<Guid> CreateInvoiceAsync(CreateInvoiceDto dto);
    Task<Guid> GenerateInvoiceFromSalesOrderAsync(Guid salesOrderId);
    Task<Guid> GenerateInvoiceFromPurchaseOrderAsync(Guid purchaseOrderId);

    /// <summary>Invoices for the credit- and debit-note pickers. Was read off the context.</summary>
    Task<List<InvoiceOptionDto>> GetRecentInvoiceOptions(int count, InvoiceType type);
}
