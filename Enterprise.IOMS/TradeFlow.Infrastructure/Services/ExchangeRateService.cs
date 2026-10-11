using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Application.Interfaces;
using TradeFlow.Domain.Entities;
using TradeFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace TradeFlow.Infrastructure.Services
{
    public class ExchangeRateService : IExchangeRateService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly IMapper _mapper;

        public ExchangeRateService(IDbContextFactory<ApplicationDbContext> dbFactory, IMapper mapper) { _dbFactory = dbFactory; _mapper = mapper; }

        public async Task<List<ExchangeRateDto>> GetExchangeRates()
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();
            var items = await _db.ExchangeRates.Include(e => e.FromCurrency).Include(e => e.ToCurrency)
                .OrderByDescending(e => e.EffectiveDate).ToListAsync();
            return _mapper.Map<List<ExchangeRateDto>>(items);
        }

        public async Task<Guid> CreateExchangeRate(Guid fromCurrencyId, Guid toCurrencyId, decimal rate, DateTime effectiveDate)
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();
            var er = new ExchangeRate { FromCurrencyId = fromCurrencyId, ToCurrencyId = toCurrencyId, Rate = rate, EffectiveDate = effectiveDate };
            _db.ExchangeRates.Add(er);
            await _db.SaveChangesAsync();
            return er.Id;
        }

        public async Task DeleteExchangeRate(Guid id)
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();
            var er = await _db.ExchangeRates.FindAsync(id) ?? throw new KeyNotFoundException("Exchange rate not found");
            er.IsDeleted = true;
            await _db.SaveChangesAsync();
        }
    }

}
