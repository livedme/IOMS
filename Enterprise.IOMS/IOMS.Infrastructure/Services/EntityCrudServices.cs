using AutoMapper;
using IOMS.Application.DTOs;
using IOMS.Application.Interfaces;
using IOMS.Domain.Entities;
using IOMS.Domain.Enums;
using IOMS.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IOMS.Infrastructure.Services
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

