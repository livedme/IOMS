using AutoMapper;
using IOMS.Application.DTOs;
using IOMS.Application.Interfaces;
using IOMS.Domain.Entities;
using IOMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace IOMS.Infrastructure.Services
{
    public class ExchangeRateService : IExchangeRateService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public ExchangeRateService(ApplicationDbContext db, IMapper mapper) { _db = db; _mapper = mapper; }

        public async Task<List<ExchangeRateDto>> GetExchangeRates()
        {
            var items = await _db.ExchangeRates.Include(e => e.FromCurrency).Include(e => e.ToCurrency)
                .OrderByDescending(e => e.EffectiveDate).ToListAsync();
            return _mapper.Map<List<ExchangeRateDto>>(items);
        }

        public async Task<Guid> CreateExchangeRate(Guid fromCurrencyId, Guid toCurrencyId, decimal rate, DateTime effectiveDate)
        {
            var er = new ExchangeRate { FromCurrencyId = fromCurrencyId, ToCurrencyId = toCurrencyId, Rate = rate, EffectiveDate = effectiveDate };
            _db.ExchangeRates.Add(er);
            await _db.SaveChangesAsync();
            return er.Id;
        }

        public async Task DeleteExchangeRate(Guid id)
        {
            var er = await _db.ExchangeRates.FindAsync(id) ?? throw new KeyNotFoundException("Exchange rate not found");
            er.IsDeleted = true;
            await _db.SaveChangesAsync();
        }
    }

}
