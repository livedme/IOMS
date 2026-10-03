using System;
using System.Collections.Generic;
using System.Text;

namespace TradeFlow.Application.DTOs
{

    public record BranchDto(Guid Id, string? Name, string? Code, string? Location, string? Address, bool IsActive);
    public record CreateBranchDto(string Name, string Code, string? Location, string? Address, bool IsActive);
}
