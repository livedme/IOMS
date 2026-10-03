using System;
using System.Collections.Generic;
using System.Text;

namespace TradeFlow.Application.DTOs
{
    // Warehouses
    public record WarehouseDto(Guid Id, string Name, string Code, string? Location, string? Address, bool IsActive);
    public record CreateWarehouseDto(string Name, string Code, string? Location, string? Address);
}
