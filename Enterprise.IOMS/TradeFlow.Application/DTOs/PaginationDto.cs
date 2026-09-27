using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.DTOs
{

    /// <summary>
    /// Generic server-side pagination request sent from UI to database layer.
    /// Page is zero-based (0 = first page) to match Blazor PaginationFooter.
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

    public class DriverPagedRequest : PagedRequest
    {
        public int? TenantId { get; set; }
        public string? Status { get; set; }
        public string? BranchName { get; set; }
    }

    public class CustomerPagedRequest : PagedRequest
    {
        public int? TenantId { get; set; }
        public string? CustomerType { get; set; } // "All" or CustomerType name
        public string? Status { get; set; } // "All" / "Active" / "Inactive"
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
        public int? TenantId { get; set; }
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
        public int? TenantId { get; set; }
        public string? Category { get; set; } // "All" or category
        public string? StockStatus { get; set; } // "All" | "Low Stock" | "In Stock"
    }
}