using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface IPricingService
{
    Task<List<PriceListDto>> GetPriceLists();
    Task<Guid> CreatePriceList(CreatePriceListDto dto);
    Task AddPriceListItem(Guid priceListId, CreatePriceListItemDto dto);
    Task<decimal> GetEffectivePrice(Guid productId, Guid? customerId, decimal quantity);
    Task<List<DiscountDto>> GetDiscounts();
    Task<Guid> CreateDiscount(CreateDiscountDto dto);
    Task<List<CurrencyDto>> GetCurrencies();
    Task<Guid> CreateCurrency(CreateCurrencyDto dto);
    Task<decimal> ConvertCurrency(decimal amount, Guid fromCurrencyId, Guid toCurrencyId);
}
