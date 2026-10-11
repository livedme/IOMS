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
    public class NotificationTemplateService : INotificationTemplateService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly IMapper _mapper;

        public NotificationTemplateService(IDbContextFactory<ApplicationDbContext> dbFactory, IMapper mapper) { _dbFactory = dbFactory; _mapper = mapper; }

        public async Task<List<NotificationTemplateDto>> GetTemplates()
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();
            var items = await _db.NotificationTemplates.OrderBy(t => t.EventType).ToListAsync();
            return _mapper.Map<List<NotificationTemplateDto>>(items);
        }

        public async Task<List<NotificationLogDto>> GetRecentLogs(int count)
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();
            return await _db.NotificationLogs
                .AsNoTracking()
                .OrderByDescending(l => l.SentAt)
                .Take(count)
                .Select(l => new NotificationLogDto(l.Id, l.UserId, l.EventType, l.Channel, l.Status, l.SentAt))
                .ToListAsync();
        }

        public async Task<Guid> CreateTemplate(CreateNotificationTemplateDto dto)
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();
            var template = new NotificationTemplate { EventType = dto.EventType, Channel = dto.Channel, Subject = dto.Subject, BodyTemplate = dto.BodyTemplate };
            _db.NotificationTemplates.Add(template);
            await _db.SaveChangesAsync();
            return template.Id;
        }

        public async Task UpdateTemplate(Guid id, CreateNotificationTemplateDto dto)
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();
            var template = await _db.NotificationTemplates.FindAsync(id) ?? throw new KeyNotFoundException("Template not found");
            template.EventType = dto.EventType; template.Channel = dto.Channel;
            template.Subject = dto.Subject; template.BodyTemplate = dto.BodyTemplate;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteTemplate(Guid id)
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();
            var template = await _db.NotificationTemplates.FindAsync(id) ?? throw new KeyNotFoundException("Template not found");
            template.IsDeleted = true;
            await _db.SaveChangesAsync();
        }
    }
}
