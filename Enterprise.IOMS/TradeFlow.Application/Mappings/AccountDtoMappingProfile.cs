using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Mappings;

public class AccountDtoMappingProfile : Profile
{
    public AccountDtoMappingProfile()
    {
        CreateMap<Account, AccountDto>()
            .ConstructUsing(s => new AccountDto(s.Id, s.Code, s.Name, s.AccountType,
                s.ParentAccountId, s.ParentAccount != null ? s.ParentAccount.Name : null,
                s.IsActive, s.IsSystemAccount, s.Description, 0))
            .ForMember(d => d.ParentAccountName, o => o.MapFrom(s => s.ParentAccount != null ? s.ParentAccount.Name : null))
            .ForMember(d => d.Balance, o => o.Ignore());
    }
}
