namespace TradeFlow.Application.DTOs
{

    //public record GlobalSearchResultDto(string EntityType, Guid Id, string Title, string SubTitle, string Url);

    /// <summary>
    /// One entity kind the global search can be restricted to. A user ticks the ones they care about
    /// and the search only runs against those, so unchecking a type drops it from both the queries
    /// and the result tabs.
    /// </summary>CategoryId
    public enum SearchEntity
    {
        Products = 0,
        Category = 1,
        Customers = 2,
        Suppliers = 3,
        SalesOrders = 4,
        PurchaseOrders = 5
    }

    public class GlobalSearchResultDto
    {
        public string EntityType { get; }
        public string Query { get; }
        public int PageSize { get; }

        public List<ProductDto> ProductItems { get; set; } = new();
        public List<CustomerDto> CustomersItems { get; set; } = new();
        public List<SupplierDto> SuppliersItems { get; set; } = new();
        public List<SalesOrderDto> SalesOrdersItems { get; set; } = new();
        public List<PurchaseOrderDto> PurchaseOrdersItems { get; set; } = new();

    }
}