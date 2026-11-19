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
    Task<RenderedDocument> RenderInvoiceAsync(InvoiceDetailDto invoiceDetail);
    Task<RenderedDocument> RenderProductAsync(ProductDetailViewDto product);
    Task<RenderedDocument> RenderCustomerAsync(CustomerDto customer, decimal outstanding = 0,
        IEnumerable<SalesOrderDto>? orders = null, IEnumerable<StatementInvoiceRowDto>? invoices = null);
    Task<RenderedDocument> RenderSupplierAsync(SupplierDto supplier, IEnumerable<PurchaseOrderDto>? orders = null);
    Task<RenderedDocument> RenderCustomerLedgerAsync(CustomerLedgerDto ledger, string customerName = "All Customers");
    Task<RenderedDocument> RenderSupplierLedgerAsync(SupplierLedgerDto ledger, string supplierName = "All Suppliers");
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

    public Task<RenderedDocument> RenderInvoiceAsync(InvoiceDetailDto invoiceDetail) =>
        RenderCoreAsync("Invoice", invoiceDetail.Invoice.InvoiceNumber, invoiceDetail.Invoice.Status.ToString(), (nameof(PrintDocumentBody.InvoiceDetail), invoiceDetail));

    public Task<RenderedDocument> RenderProductAsync(ProductDetailViewDto product) =>
        RenderCoreAsync("Product Details", product.SKU ?? "—", "", (nameof(PrintDocumentBody.Product), product));

    public Task<RenderedDocument> RenderCustomerAsync(CustomerDto customer, decimal outstanding = 0,
        IEnumerable<SalesOrderDto>? orders = null, IEnumerable<StatementInvoiceRowDto>? invoices = null) =>
        RenderCoreAsync("Customer Details", customer.CustomerName, customer.IsActive ? "Active" : "Inactive",
            (nameof(PrintDocumentBody.Customer), customer),
            new Dictionary<string, object?>
            {
                [nameof(PrintDocumentBody.CustomerOutstanding)] = outstanding,
                [nameof(PrintDocumentBody.CustomerOrders)] = orders?.ToList() ?? new List<SalesOrderDto>(),
                [nameof(PrintDocumentBody.CustomerInvoices)] = invoices?.ToList() ?? new List<StatementInvoiceRowDto>()
            });

    public Task<RenderedDocument> RenderSupplierAsync(SupplierDto supplier, IEnumerable<PurchaseOrderDto>? orders = null) =>
        RenderCoreAsync("Supplier Details", supplier.SupplierName, supplier.IsActive ? "Active" : "Inactive",
            (nameof(PrintDocumentBody.Supplier), supplier),
            new Dictionary<string, object?>
            {
                [nameof(PrintDocumentBody.SupplierOrders)] = orders?.ToList() ?? new List<PurchaseOrderDto>()
            });

    public Task<RenderedDocument> RenderCustomerLedgerAsync(CustomerLedgerDto ledger, string customerName = "All Customers")
    {
        var name = string.IsNullOrWhiteSpace(customerName) ? "All Customers" : customerName;
        return RenderCoreAsync("Customer Ledger", name,
            $"{(ledger.From?.ToString("dd MMM yyyy") ?? "All")} - {(ledger.To?.ToString("dd MMM yyyy") ?? "Today")}",
            (nameof(PrintDocumentBody.CustomerLedger), ledger),
            new Dictionary<string, object?>
            {
                [nameof(PrintDocumentBody.LedgerCustomerName)] = name
            });
    }

    public Task<RenderedDocument> RenderSupplierLedgerAsync(SupplierLedgerDto ledger, string supplierName = "All Suppliers")
    {
        var name = string.IsNullOrWhiteSpace(supplierName) ? "All Suppliers" : supplierName;
        return RenderCoreAsync("Supplier Ledger", name,
            $"{(ledger.From?.ToString("dd MMM yyyy") ?? "All")} - {(ledger.To?.ToString("dd MMM yyyy") ?? "Today")}",
            (nameof(PrintDocumentBody.SupplierLedger), ledger),
            new Dictionary<string, object?>
            {
                [nameof(PrintDocumentBody.LedgerSupplierName)] = name
            });
    }

    private async Task<RenderedDocument> RenderCoreAsync(string title, string reference, string statusText, (string Key, object? Value) document, Dictionary<string, object?>? extraParameters = null)
    {
        await using var renderer = new HtmlRenderer(_services, _loggerFactory);

        var documentParameters = new Dictionary<string, object?>
        {
            [nameof(PrintDocumentBody.DocTitle)] = title,
            [nameof(PrintDocumentBody.DocRef)] = reference,
            [nameof(PrintDocumentBody.StatusText)] = statusText,
            [document.Key] = document.Value
        };

        if (extraParameters is not null)
        {
            foreach (var pair in extraParameters)
            {
                documentParameters[pair.Key] = pair.Value;
            }
        }

        var parameters = ParameterView.FromDictionary(documentParameters);

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<PrintDocumentBody>(parameters);
            return output.ToHtmlString();
        });

        return new RenderedDocument(html, title, reference, BuildFileName(title, reference));
    }

    private static string BuildFileName(string title, string reference)
    {
        var fileName = title + " " + (reference == "—" ? "" : reference);
        fileName = new string(fileName.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == ' ').ToArray()).Trim();
        return string.IsNullOrWhiteSpace(fileName) ? "document" : fileName;
    }
}