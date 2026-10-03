using System;
using System.Collections.Generic;
using System.Text;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.DTOs
{

    // Tax
    public record TaxRateDto(Guid Id, string Name, decimal Rate, TaxType TaxType,
        Guid? TaxJurisdictionId, string? JurisdictionName, DateTime EffectiveFrom,
        DateTime? EffectiveTo, bool IsActive, bool IsCompound);

    public record CreateTaxRateDto(string Name, string? Code, decimal Rate, TaxType TaxType,
        bool IsCompound, bool IsActive = true, Guid? TaxJurisdictionId = null);

    public record TaxJurisdictionDto(Guid Id, string Name, string Code, string? Country,
        string? State, List<TaxRateDto> TaxRates);

    public record TaxCalculationResult(decimal Amount, decimal TaxAmount, decimal TaxRate, string? TaxName, Guid? TaxRateId);

    // Tax Jurisdiction
    public record CreateTaxJurisdictionDto(string Name, string Code, string Country, string? State);


}
