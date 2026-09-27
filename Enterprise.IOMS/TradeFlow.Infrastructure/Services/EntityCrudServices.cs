using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Application.Interfaces;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace TradeFlow.Infrastructure.Services
{
    public interface ISharedNumberGenerator
    {
        string Generate(string prefix);
    }

    public class SharedNumberGenerator : ISharedNumberGenerator
    {
        public string Generate(string prefix) => $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
    }

}

