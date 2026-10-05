using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using TradeFlow.Application.DTOs;
using TradeFlow.Application.Interfaces;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Exceptions;
using TradeFlow.Infrastructure.Data;

namespace TradeFlow.Infrastructure.Services
{
    public class BranchService : IBranchService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public BranchService(ApplicationDbContext context, IMapper mapper, IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _context = context;
            _mapper = mapper;
            _contextFactory = contextFactory;
        }

        public async Task<PagedResultNew<BranchDto>> GetBranchesPagedAsync(BranchPagedRequest request)
        {
            // Own context for the whole read: the list can be re-entered while another query on the
            // scoped context is still in flight, and a DbContext cannot run two commands at once.
            await using var read = await _contextFactory.CreateDbContextAsync();
            var response = new PagedResultNew<BranchDto>();

            var page = Math.Max(0, request.CurrentPage);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var search = request.SearchTerm?.Trim();

            var query = read.Branches.AsNoTracking();
            if (request.TenantId.HasValue && request.TenantId != Guid.Empty)
                query = query.Where(b => b.TenantId == request.TenantId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(b =>
                    EF.Functions.Like(b.Name, $"%{search}%") ||
                    EF.Functions.Like(b.Code, $"%{search}%") ||
                    EF.Functions.Like(b.Location, $"%{search}%") ||
                    EF.Functions.Like(b.Address, $"%{search}%"));
            }

            // Stats ignore the active filter so the tiles keep showing both buckets.
            var statRows = await query
                .GroupBy(b => b.IsActive)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync();

            response.Stats["TotalCount"] = statRows.Sum(r => r.Count);
            response.Stats["ActiveCount"] = statRows.Where(r => r.Key).Sum(r => r.Count);
            response.Stats["InactiveCount"] = statRows.Where(r => !r.Key).Sum(r => r.Count);
            response.Stats["WithLocationCount"] = await query
                .CountAsync(b => b.Location != null && b.Location != string.Empty);
            response.Stats["UniqueCodeCount"] = await query.Select(b => b.Code).Distinct().CountAsync();

            if (request.IsActive.HasValue)
                query = query.Where(b => b.IsActive == request.IsActive.Value);

            var sortAsc = request.SortAscending;
            query = (request.SortColumn ?? "Name") switch
            {
                "Code" => sortAsc ? query.OrderBy(b => b.Code) : query.OrderByDescending(b => b.Code),
                "Location" => sortAsc ? query.OrderBy(b => b.Location) : query.OrderByDescending(b => b.Location),
                "Address" => sortAsc ? query.OrderBy(b => b.Address) : query.OrderByDescending(b => b.Address),
                "IsActive" => sortAsc ? query.OrderBy(b => b.IsActive) : query.OrderByDescending(b => b.IsActive),
                _ => sortAsc ? query.OrderBy(b => b.Name) : query.OrderByDescending(b => b.Name),
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            response.Items = items
                .Select(b => new BranchDto(b.Id, b.Name, b.Code, b.Location, b.Address, b.IsActive))
                .ToList();
            response.TotalCount = totalCount;
            response.CurrentPage = page;
            response.PageSize = pageSize;

            return response;
        }
        public async Task<Guid> CreateBranch(CreateBranchDto dto)
        {

            var branch = new Branch()
            {
                Name = dto.Name,
                Code = dto.Code,
                Location = dto.Location,
                Address = dto.Address,
                IsActive = dto.IsActive
            };

            await _context.Branches.AddAsync(branch);
            await _context.SaveChangesAsync();

            return branch.Id;
        }

        public async Task DeleteBranch(Guid id)
        {
            var entity = await _context.Branches.FindAsync(id) ?? throw new EntityNotFoundException("Branches", id);
            if (entity != null)
            {
                _context.Branches.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<BranchDto?> GetBranchById(Guid id)
        {
            var query = await _context.Branches
               .Include(o => o.SalesOrders).FirstOrDefaultAsync(o => o.Id == id) ?? throw new EntityNotFoundException("Branches", id);

            var response = _mapper.Map<BranchDto>(query);

            return response;
        }

        public async Task<List<BranchDto>> GetBranches()
        {
            var query = _context.Branches
                .Include(o => o.SalesOrders)
                .AsQueryable();

            var response = _mapper.Map<List<BranchDto>>(query.ToList());

            return response ?? new List<BranchDto>();
        }

        /// <summary>
        /// Active branches for the order filters. Orders.razor was reading this straight off the
        /// context; the projection here matches the one it was doing by hand.
        /// </summary>
        public async Task<List<BranchDto>> GetActiveBranchesAsync() =>
            await _context.Branches
                .AsNoTracking()
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .Select(b => new BranchDto(b.Id, b.Name, b.Code, b.Location, b.Address, b.IsActive))
                .ToListAsync();

        public async Task UpdateBranch(Guid id, CreateBranchDto dto)
        {
            var entity = await _context.Branches.FindAsync(id) ?? throw new EntityNotFoundException("Branches", id);
            if (entity != null)
            {
                entity.Name = dto.Name;
                entity.Code = dto.Code;
                entity.Location = dto.Location;
                entity.Address = dto.Address;
                entity.IsActive = dto.IsActive;
                await _context.SaveChangesAsync();
            }
        }
    }
}
