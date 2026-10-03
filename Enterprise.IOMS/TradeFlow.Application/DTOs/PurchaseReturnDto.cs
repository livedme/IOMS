using TradeFlow.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace TradeFlow.Application.DTOs
{
    public class PurchaseReturnDto
    {
        public Guid Id { get; set; }
        public string ReturnNumber { get; set; }
        public Guid PurchaseOrderId { get; set; }
        public string PurchaseOrderNumber { get; set; }
        public PurchaseReturnStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string Reason { get; set; }
        public ControlDto PurchaseReturnDdlControl { get; set; } = new ControlDto();

        public List<PurchaseReturnItemDto> ItemsLine { get; set; }= new List<PurchaseReturnItemDto>();

        public class PurchaseReturnItemDto
        {            
            public Guid Id { get; set; }
            public Guid ProductId { get; set; }
            public string ProductName { get; set; }
            public int Quantity { get; set; }
            public decimal UnitCost { get; set; }
            public decimal LineTotal { get; set; }
            public bool IsSelected { get; set; }
        }
    }

    // Purchase Returns
    //public record PurchaseReturnDto(Guid Id, string ReturnNumber, Guid PurchaseOrderId,
    //    string PurchaseOrderNumber, PurchaseReturnStatus Status, decimal TotalAmount,
    //    string? Reason, List<PurchaseReturnItemDto> Items);

    //public record PurchaseReturnItemDto(Guid Id, Guid ProductId, string ProductName,
    //    int Quantity, decimal UnitCost, decimal LineTotal);

    //public record CreatePurchaseReturnDto(Guid PurchaseOrderId, string? Reason,
    //    List<CreatePurchaseReturnItemDto> Items);

    //public record CreatePurchaseReturnItemDto(Guid ProductId, int Quantity, decimal UnitCost);

}
