using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LogSentinel.BUS.Models;
using LogSentinel.DAL.Data;

namespace LogSentinel.BUS.Interfaces
{
    public interface IRuleEngine
    {
        Task InitializeAsync();
        Task<bool> EvaluateEventAsync(EventEntity evt);
        Task<IEnumerable<EventEntity>> EvaluateBatchAsync(DateTime startTime, DateTime endTime);
        Task ReloadRulesAsync();
    }
}
