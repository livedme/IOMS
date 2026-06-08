using IOMS.Domain.Enums;
using MudBlazor;

namespace IOMS.Web
{
    public class CommonUtility
    {
        public static Color GetStatusColor(OrderStatus status) => status switch
        {
            OrderStatus.Pending => Color.Default,
            OrderStatus.Approved => Color.Info,
            OrderStatus.Sold => Color.Success,
            OrderStatus.Packed => Color.Secondary,
            OrderStatus.Shipped => Color.Primary,
            OrderStatus.PartiallyShipped => Color.Info,
            OrderStatus.Delivered => Color.Warning,
            OrderStatus.Cancelled => Color.Error,
            _ => Color.Default
        }; 
         public static  Color GetStatusColor(DataSubjectRequestStatus s) => s switch
        {
            DataSubjectRequestStatus.Pending => Color.Warning,
            DataSubjectRequestStatus.InProgress => Color.Info,
            DataSubjectRequestStatus.Completed => Color.Success,
            DataSubjectRequestStatus.Cancelled => Color.Error,
            _ => Color.Default
        };

         public static  Color GetColor(ApprovalDecision d) => d switch
        {
            ApprovalDecision.Pending => Color.Warning,
            ApprovalDecision.Approved => Color.Success,
            ApprovalDecision.Rejected => Color.Error,
            ApprovalDecision.Escalated => Color.Info,
            _ => Color.Default
        };
         public static  Color GetStatusColor(string status) => status switch
        {
            "Pending" or "Draft" => Color.Default,
            "Confirmed" or "Submitted" or "Approved" => Color.Info,
            "Processing" or "Shipped" or "Received" or "PartiallyReceived" => Color.Warning,
            "Delivered" or "Completed" or "Invoiced" => Color.Success,
            "Cancelled" or "Rejected" => Color.Error,
            _ => Color.Default
        };

         public static  Color GetColor(StocktakeStatus s) => s switch
        {
            StocktakeStatus.Draft => Color.Default,
            StocktakeStatus.InProgress => Color.Info,
            StocktakeStatus.PendingApproval => Color.Warning,
            StocktakeStatus.Approved => Color.Success,
            _ => Color.Error
        };
         public static  Color GetStatusColor(PurchaseOrderStatus status) => status switch
        {
            PurchaseOrderStatus.Draft => Color.Default,
            PurchaseOrderStatus.Approved => Color.Info,
            PurchaseOrderStatus.Submitted => Color.Primary,
            PurchaseOrderStatus.PartiallyReceived => Color.Warning,
            PurchaseOrderStatus.Received => Color.Success,
            PurchaseOrderStatus.Closed => Color.Dark,
            PurchaseOrderStatus.Cancelled => Color.Error,
            _ => Color.Default
        };

         public static  Color GetStatusColor(RfqStatus s) => s switch
        {
            RfqStatus.Draft => Color.Default,
            RfqStatus.Sent => Color.Info,
            RfqStatus.Received => Color.Warning,
            RfqStatus.Awarded => Color.Success,
            RfqStatus.Closed => Color.Dark,
            _ => Color.Error
        };        
         public static  Color GetTypeColor(string type) => type switch
        {
            "Product" => Color.Primary,
            "Customer" => Color.Success,
            "Supplier" => Color.Info,
            "SalesOrder" => Color.Warning,
            "PurchaseOrder" => Color.Secondary,
            "Invoice" => Color.Error,
            _ => Color.Default
        };
         public static  Color GetColor(ShipmentStatus s) => s switch
        {
            ShipmentStatus.Pending => Color.Default,
            ShipmentStatus.InTransit => Color.Info,
            ShipmentStatus.Delivered => Color.Success,
            ShipmentStatus.Failed => Color.Error,
            ShipmentStatus.Returned => Color.Warning,
            _ => Color.Default
        };
    }
}
