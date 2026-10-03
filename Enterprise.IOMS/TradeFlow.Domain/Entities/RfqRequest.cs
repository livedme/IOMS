using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class RfqRequest : BaseEntity
{
    public string RfqNumber { get; set; } = string.Empty;
    public RfqStatus Status { get; set; } = RfqStatus.Draft;
    public DateTime? RequiredDate { get; set; }
    public string? Notes { get; set; }
    public ICollection<RfqItem> Items { get; set; } = new List<RfqItem>();
    public ICollection<RfqSupplierResponse> SupplierResponses { get; set; } = new List<RfqSupplierResponse>();
}
