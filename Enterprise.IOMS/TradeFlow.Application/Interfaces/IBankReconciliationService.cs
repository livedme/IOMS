using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface IBankReconciliationService
{
    Task<List<BankStatementDto>> GetBankStatements();
    Task<BankStatementDto> GetBankStatementById(Guid id);
    Task<Guid> ImportBankStatement(CreateBankStatementDto dto, List<CreateBankStatementLineDto> lines);
    Task MatchLine(Guid bankStatementLineId, Guid paymentId);
    Task UnmatchLine(Guid bankStatementLineId);
    Task ApproveReconciliation(Guid bankStatementId);
}
