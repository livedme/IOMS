using IOMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace IOMS.Application.DTOs
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
}
