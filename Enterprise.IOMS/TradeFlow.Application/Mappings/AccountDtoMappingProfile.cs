using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Mappings;

public class AccountDtoMappingProfile : Profile
{
    public AccountDtoMappingProfile()
    {
        CreateMap<Account, AccountModelDto>()
            .ConstructUsing(s => new AccountModelDto()
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                AccountType = s.AccountType,
                ParentAccountId = s.ParentAccountId,
                ParentAccountName = s.ParentAccount != null ? s.ParentAccount.Name : null,
                IsActive = s.IsActive,
                IsSystemAccount = s.IsSystemAccount,
                Description = s.Description,
                Balance = 0
            })
            .ForMember(d => d.ParentAccountName, o => o.MapFrom(s => s.ParentAccount != null ? s.ParentAccount.Name : null))
            .ForMember(d => d.Balance, o => o.Ignore());
    }
}
