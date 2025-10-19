using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogSentinel.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace LogSentinel.DAL.Repositories
{
    public interface IAlertRepository
    {
        Task<AlertEntity?> GetByIdAsync(long id);
        Task<IEnumerable<AlertEntity>> GetAllAsync();
        Task<IEnumerable<AlertEntity>> GetRecentAsync(int minutes = 5);
        Task<IEnumerable<AlertEntity>> GetUnacknowledgedAsync();
        Task<AlertEntity> AddAsync(AlertEntity entity);
        Task UpdateAsync(AlertEntity entity);
        Task DeleteAsync(AlertEntity entity);
        Task<int> CountRecentAsync(int minutes = 5);
    }

    public class AlertRepository : EFRepository<AlertEntity>, IAlertRepository
    {
        public AlertRepository(AppDbContext context) : base(context)
        {
        }

        public override async Task<IEnumerable<AlertEntity>> GetAllAsync()
        {
            return await _dbSet
                .Include(a => a.Rule)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
        }

        public override async Task<AlertEntity?> GetByIdAsync(long id)
        {
            return await _dbSet
                .Include(a => a.Rule)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<AlertEntity>> GetRecentAsync(int minutes = 5)
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-minutes);
            return await _dbSet
                .Include(a => a.Rule)
                .Where(a => a.Timestamp >= cutoff)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<AlertEntity>> GetUnacknowledgedAsync()
        {
            return await _dbSet
                .Include(a => a.Rule)
                .Where(a => !a.IsAcknowledged)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
        }

        public async Task<int> CountRecentAsync(int minutes = 5)
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-minutes);
            return await _dbSet.CountAsync(a => a.Timestamp >= cutoff);
        }
    }
}
