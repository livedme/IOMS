namespace TradeFlow.Application.DTOs
{

    //public record GlobalSearchResultDto(string EntityType, Guid Id, string Title, string SubTitle, string Url);

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