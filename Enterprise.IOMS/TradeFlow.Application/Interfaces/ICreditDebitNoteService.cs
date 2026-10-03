using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface ICreditDebitNoteService
{
    Task<PagedResult<CreditNoteDto>> GetCreditNotes(string? search, int page, int pageSize);
    Task<Guid> CreateCreditNote(CreateCreditNoteDto dto);
    Task ApproveCreditNote(Guid id);
    Task<PagedResult<DebitNoteDto>> GetDebitNotes(string? search, int page, int pageSize);
    Task<Guid> CreateDebitNote(CreateDebitNoteDto dto);
    Task ApproveDebitNote(Guid id);
}
