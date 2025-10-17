using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogSentinel.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace LogSentinel.DAL.Repositories
{
    public interface IRuleRepository
    {
        Task<RuleEntity?> GetByIdAsync(long id);
        Task<IEnumerable<RuleEntity>> GetAllAsync();
        Task<IEnumerable<RuleEntity>> GetEnabledAsync();
        Task<RuleEntity?> GetByNameAsync(string name);
        Task<RuleEntity> AddAsync(RuleEntity entity);
        Task UpdateAsync(RuleEntity entity);
        Task DeleteAsync(RuleEntity entity);
    }

    public class RuleRepository : EFRepository<RuleEntity>, IRuleRepository
    {
        public RuleRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<RuleEntity>> GetEnabledAsync()
        {
            return await _dbSet
                .Where(r => r.IsEnabled)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<RuleEntity?> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(r => r.Name == name);
        }
    }
}
