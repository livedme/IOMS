using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Interfaces;

public interface IQuotationService
{
    Task<PagedResult<SalesQuoteDto>> GetSalesQuotes(string? search, QuoteStatus? status, int page, int pageSize);
    Task<PagedResultNew<SalesQuoteDto>> GetSalesQuotesAsync(QuotationPagedRequest request);
    Task<SalesQuoteDto> GetSalesQuoteById(Guid id);
    Task<Guid> CreateSalesQuote(CreateSalesQuoteDto dto);
    Task<Guid> ConvertQuoteToOrder(Guid quoteId);
    Task AcceptQuote(Guid quoteId);
    Task RejectQuote(Guid quoteId, string reason);
}
