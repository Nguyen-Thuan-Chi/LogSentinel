using System.Collections.Generic;
using System.Threading.Tasks;
using LogSentinel.BUS.Models;

namespace LogSentinel.BUS.Interfaces
{
    public interface IRuleProvider
    {
        Task<IEnumerable<RuleDefinition>> LoadRulesAsync();
        Task<RuleDefinition?> GetRuleByNameAsync(string name);
        Task SaveRuleAsync(RuleDefinition rule);
    }
}
