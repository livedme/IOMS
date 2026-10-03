using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.DTOs
{

    /// <summary>
    /// Generic server-side pagination request sent from UI to database layer.
    /// Page is zero-based (0 = first page) to match Blazor TmPager.
    /// </summary>
    public class PagedRequest
    {
        public int CurrentPage { get; set; } = 0;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortColumn { get; set; }
        public bool SortAscending { get; set; } = true;
    }

    // Pagination    
    /// <summary>
    /// Server-side paged result returned from EF Core query.
    /// Translates to Skip/Take at SQL level so only the requested page is loaded.
    /// </summary>
    public class PagedResultNew<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
        public Dictionary<string, int>? Stats { get; set; } = new();
    }

    // Entity-specific filtered requests — inherit base paging + search + sort

    public class ProductPagedRequest : PagedRequest
    {
        public Guid? TenantId { get; set; }
        public StockFilter? Status { get; set; } // "All" or TruckStatus name
        public Guid? CategoryId { get; set; } // "All" or type
        public Guid? BrandId { get; set; } // "All" or branch name
    }

    public class SalesPagedRequest : PagedRequest
    {
        public Guid? TenantId { get; set; }
        /// <summary>Single status filter applied on top of <see cref="StatusScope"/>.</summary>
        public OrderStatus? Status { get; set; }
        /// <summary>Base set of statuses the page is scoped to. Null/empty means every status.</summary>
        public List<OrderStatus>? StatusScope { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? BranchId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    public class DriverPagedRequest : PagedRequest
    {
        public int? TenantId { get; set; }
        public string? Status { get; set; }
        public string? BranchName { get; set; }
    }

    public class CustomerPagedRequest : PagedRequest
    {
        public Guid? TenantId { get; set; }
        public string? CustomerType { get; set; } // "All" or CustomerType name
        public string? Status { get; set; } // "All" / "Active" / "Inactive"
        public bool? IsActive { get; set; }
        public string? City { get; set; }
    }

    public class QuotationPagedRequest : PagedRequest
    {
        public Guid? TenantId { get; set; }
        public QuoteStatus? Status { get; set; }
        public Guid? CustomerId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    public class SalesReturnPagedRequest : PagedRequest
    {
        public Guid? TenantId { get; set; }
        public SalesReturnStatus? Status { get; set; }
        public Guid? SalesOrderId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    public class TripPagedRequest : PagedRequest
    {
        public int? TenantId { get; set; }
        public string? Status { get; set; }
        public int? TruckId { get; set; }
    }

    public class RentalPagedRequest : PagedRequest
    {
        public int? TenantId { get; set; }
        public string? Status { get; set; }
        public string? RentalType { get; set; } // "All" or RentalType name — mirrors RentalsPage filter
    }

    public class FuelLogPagedRequest : PagedRequest
    {
        public int? TenantId { get; set; }
        public int? TruckId { get; set; }
        public string? TruckNo { get; set; } // UI filters by TruckNo string (All or TruckNo)
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    public class MaintenancePagedRequest : PagedRequest
    {
        public int? TenantId { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; } // All or MaintenancePriority
        public string? MaintenanceType { get; set; } // All or MaintenanceType
        public int? TruckId { get; set; }
    }

    public class VendorPagedRequest : PagedRequest
    {
        public int? TenantId { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; } // All / Active / Inactive — mirrors VendorsPage
    }

    public class BranchPagedRequest : PagedRequest
    {
        public Guid? TenantId { get; set; }
        public bool? IsActive { get; set; }
    }

    public class WarehousePagedRequest : PagedRequest
    {
        public Guid? TenantId { get; set; }
        public bool? IsActive { get; set; }
    }

    public class BrandPagedRequest : PagedRequest
    {
        public Guid? TenantId { get; set; }
        /// <summary>"All" / "Active" / "Inactive" — matches the Brand.Status string column.</summary>
        public string? Status { get; set; }
        public bool? HasLogo { get; set; }
        public string? OriginCountry { get; set; }
        /// <summary>"All" / "With Products" / "Without Products".</summary>
        public string? ProductFilter { get; set; }
    }

    public class CategoryPagedRequest : PagedRequest
    {
        public Guid? TenantId { get; set; }
        /// <summary>Null = every level, 1 = top level (no parent), 2 = has a parent.</summary>
        public int? Level { get; set; }
        /// <summary>"All" / "With Products" / "Without Products".</summary>
        public string? ProductFilter { get; set; }
    }

    public class KitPagedRequest : PagedRequest
    {
        public Guid? TenantId { get; set; }
        public bool? IsActive { get; set; }
    }

    public class StocktakePagedRequest : PagedRequest
    {
        public Guid? TenantId { get; set; }
        public StocktakeStatus? Status { get; set; }
        public Guid? WarehouseId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    /// <summary>
    /// Backs both the Stock Movements grid (Type left null) and the Stock Transfers
    /// grid (Type pinned to <see cref="StockMovementType.Transfer"/>).
    /// </summary>
    public class StockMovementPagedRequest : PagedRequest
    {
        public Guid? TenantId { get; set; }
        public StockMovementType? Type { get; set; }
        public Guid? WarehouseId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    /// <summary>Backs the Stock Levels grid and its Low Stock Alerts view.</summary>
    public class StockLevelPagedRequest : PagedRequest
    {
        public Guid? TenantId { get; set; }
        public Guid? WarehouseId { get; set; }
        /// <summary>"All" / "InStock" / "OutOfStock" / "Reserved".</summary>
        public string? Status { get; set; }
        /// <summary>Pin to rows at or below the product's reorder level (Low Stock Alerts view).</summary>
        public bool LowStockOnly { get; set; }
    }

    public class InvoicePagedRequest : PagedRequest
    {
        public int? TenantId { get; set; }
        public string? Status { get; set; }
        public string? InvoiceType { get; set; } // "All" or InvoiceType name — mirrors BillingPage filter
    }

    public class TripExpensePagedRequest : PagedRequest
    {
        public int? TenantId { get; set; }
        public string? ExpenseType { get; set; } // "All" or ExpenseType name
        public string? TripNo { get; set; } // "All" or specific TripNo
    }

    public class SparePartPagedRequest : PagedRequest
    {
        public Guid? TenantId { get; set; }
        public string? Category { get; set; } // "All" or category
        public string? Status { get; set; } // "All" / "In Stock" / "Out of Stock"
    }

    public class PurchaseOrderPagedRequest : PagedRequest
    {
        public Guid? TenantId { get; set; }
        public PurchaseOrderStatus? Status { get; set; }
        public Guid? SupplierId { get; set; }
        public Guid? WarehouseId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    public class PurchaseReturnPagedRequest : PagedRequest
    {
        public Guid? TenantId { get; set; }
        public PurchaseReturnStatus? Status { get; set; }
        public Guid? PurchaseOrderId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    public class SupplierPagedRequest : PagedRequest
    {
        public Guid? TenantId { get; set; }
        public bool? IsActive { get; set; }
        public string? City { get; set; }
    }

    public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize)
    {
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }

}
