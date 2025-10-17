using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogSentinel.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace LogSentinel.DAL.Repositories
{
    public interface IEventRepository
    {
        Task<EventEntity?> GetByIdAsync(long id);
        Task<IEnumerable<EventEntity>> GetAllAsync();
        Task<IEnumerable<EventEntity>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<EventEntity>> SearchAsync(string searchTerm, int skip = 0, int take = 100);
        Task<IEnumerable<EventEntity>> GetByFilterAsync(string? host, string? user, int? eventId, string? level, DateTime? startDate, DateTime? endDate, int skip = 0, int take = 100);
        Task<EventEntity> AddAsync(EventEntity entity);
        Task AddRangeAsync(IEnumerable<EventEntity> entities);
        Task<int> CountTodayAsync();
        Task<int> CountByLevelAsync(string level);
        Task<Dictionary<string, int>> GetEventCountsByHourAsync(DateTime date);
    }

    public class EventRepository : EFRepository<EventEntity>, IEventRepository
    {
        public EventRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<EventEntity>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(e => e.EventTime >= startDate && e.EventTime <= endDate)
                .OrderByDescending(e => e.EventTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<EventEntity>> SearchAsync(string searchTerm, int skip = 0, int take = 100)
        {
            // Try FTS5 search first (SQLite only)
            try
            {
                var sql = @"
                    SELECT e.* FROM Events e
                    INNER JOIN EventsFTS fts ON e.Id = fts.Id
                    WHERE EventsFTS MATCH {0}
                    ORDER BY e.EventTime DESC
                    LIMIT {1} OFFSET {2}";
                
                return await _context.Events
                    .FromSqlRaw(sql, searchTerm, take, skip)
                    .ToListAsync();
            }
            catch
            {
                // Fallback to LIKE search (for SQL Server or if FTS not available)
                var term = searchTerm.ToLower();
                return await _dbSet
                    .Where(e => e.User.ToLower().Contains(term)
                        || e.Host.ToLower().Contains(term)
                        || e.Process.ToLower().Contains(term)
                        || e.Action.ToLower().Contains(term)
                        || e.DetailsJson.ToLower().Contains(term))
                    .OrderByDescending(e => e.EventTime)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();
            }
        }

        public async Task<IEnumerable<EventEntity>> GetByFilterAsync(
            string? host, 
            string? user, 
            int? eventId, 
            string? level, 
            DateTime? startDate, 
            DateTime? endDate,
            int skip = 0, 
            int take = 100)
        {
            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrEmpty(host))
                query = query.Where(e => e.Host.Contains(host));

            if (!string.IsNullOrEmpty(user))
                query = query.Where(e => e.User.Contains(user));

            if (eventId.HasValue)
                query = query.Where(e => e.EventId == eventId.Value);

            if (!string.IsNullOrEmpty(level))
                query = query.Where(e => e.Level == level);

            if (startDate.HasValue)
                query = query.Where(e => e.EventTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(e => e.EventTime <= endDate.Value);

            return await query
                .OrderByDescending(e => e.EventTime)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> CountTodayAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            return await _dbSet.CountAsync(e => e.EventTime >= today && e.EventTime < tomorrow);
        }

        public async Task<int> CountByLevelAsync(string level)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            return await _dbSet.CountAsync(e => e.Level == level && e.EventTime >= today && e.EventTime < tomorrow);
        }

        public async Task<Dictionary<string, int>> GetEventCountsByHourAsync(DateTime date)
        {
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1);

            var events = await _dbSet
                .Where(e => e.EventTime >= startOfDay && e.EventTime < endOfDay)
                .Select(e => e.EventTime.Hour)
                .ToListAsync();

            return events
                .GroupBy(hour => hour)
                .ToDictionary(g => $"{g.Key:D2}:00", g => g.Count());
        }
    }
}
