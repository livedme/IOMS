using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using TradeFlow.Application.DTOs;
using TradeFlow.Web.Components.Pages.Print;

namespace TradeFlow.Web.Services;

public sealed record RenderedDocument(string Html, string Title, string Reference, string FileName);

public interface IPrintDocumentRenderer
{
    Task<RenderedDocument> RenderSalesOrderAsync(SalesOrderDto order);
    Task<RenderedDocument> RenderPurchaseOrderAsync(PurchaseOrderDto order);
    Task<RenderedDocument> RenderQuoteAsync(SalesQuoteDto quote);
    Task<RenderedDocument> RenderSalesReturnAsync(SalesReturnDto salesReturn);
    Task<RenderedDocument> RenderPurchaseReturnAsync(PurchaseReturnDto purchaseReturn);
    Task<RenderedDocument> RenderInvoiceAsync(InvoiceDto invoice);
    Task<RenderedDocument> RenderProductAsync(ProductDetailViewDto product);
}

public sealed class PrintDocumentRenderer : IPrintDocumentRenderer
{
    private readonly IServiceProvider _services;
    private readonly ILoggerFactory _loggerFactory;

    public PrintDocumentRenderer(IServiceProvider services, ILoggerFactory loggerFactory)
    {
        _services = services;
        _loggerFactory = loggerFactory;
    }

    public Task<RenderedDocument> RenderSalesOrderAsync(SalesOrderDto order) =>
        RenderCoreAsync("Sales Order", order.OrderNumber, order.Status.ToString(), (nameof(PrintDocumentBody.SalesOrder), order));

    public Task<RenderedDocument> RenderPurchaseOrderAsync(PurchaseOrderDto order) =>
        RenderCoreAsync("Purchase Order", order.OrderNumber, order.Status.ToString(), (nameof(PrintDocumentBody.PurchaseOrder), order));

    public Task<RenderedDocument> RenderQuoteAsync(SalesQuoteDto quote) =>
        RenderCoreAsync("Sales Quotation", quote.QuoteNumber, quote.Status.ToString(), (nameof(PrintDocumentBody.Quote), quote));

    public Task<RenderedDocument> RenderSalesReturnAsync(SalesReturnDto salesReturn) =>
        RenderCoreAsync("Sales Return", salesReturn.ReturnNumber, salesReturn.Status.ToString(), (nameof(PrintDocumentBody.SalesReturn), salesReturn));

    public Task<RenderedDocument> RenderPurchaseReturnAsync(PurchaseReturnDto purchaseReturn) =>
        RenderCoreAsync("Purchase Return", purchaseReturn.ReturnNumber, purchaseReturn.Status.ToString(), (nameof(PrintDocumentBody.PurchaseReturn), purchaseReturn));

    public Task<RenderedDocument> RenderInvoiceAsync(InvoiceDto invoice) =>
        RenderCoreAsync("Invoice", invoice.InvoiceNumber, invoice.Status.ToString(), (nameof(PrintDocumentBody.Invoice), invoice));

    public Task<RenderedDocument> RenderProductAsync(ProductDetailViewDto product) =>
        RenderCoreAsync("Product Details", product.SKU ?? "—", "", (nameof(PrintDocumentBody.Product), product));

    private async Task<RenderedDocument> RenderCoreAsync(string title, string reference, string statusText, (string Key, object? Value) document)
    {
        await using var renderer = new HtmlRenderer(_services, _loggerFactory);

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(PrintDocumentBody.DocTitle)] = title,
            [nameof(PrintDocumentBody.DocRef)] = reference,
            [nameof(PrintDocumentBody.StatusText)] = statusText,
            [document.Key] = document.Value
        });

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<PrintDocumentBody>(parameters);
            return output.ToHtmlString();
        });

        var fileName = title + " " + (reference == "—" ? "" : reference);
        fileName = new string(fileName.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == ' ').ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(fileName)) fileName = "document";

        return new RenderedDocument(html, title, reference, fileName);
    }
}