using IOMS.Domain.Enums;

namespace IOMS.Application.DTOs;

public class SalesReturnDto
{
    public Guid Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public Guid SalesOrderId { get; set; }
    public string SalesOrderNumber { get; set; } = string.Empty;
    public SalesReturnStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Reason { get; set; }
    public ControlDto SalesReturnDdlControl { get; set; } = new();
    public List<SalesReturnItemDto> ItemsLine { get; set; } = new();
}

public class SalesReturnItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsSelected { get; set; }
}
