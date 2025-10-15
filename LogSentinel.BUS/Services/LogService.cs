using LogSentinel.DAL.Data;
using LogSentinel.DAL.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogSentinel.BUS.Interfaces;
using LogSentinel.BUS.Models;

namespace LogSentinel.BUS.Services
{
    public class LogService : ILogService
    {
        private readonly ILogRepository _repo;

        public LogService()
        {
            _repo = new LogRepository(); // sau này có thể inject
        }

        public List<LogEntry> GetRecentLogs(int count = 20)
        {
            var records = _repo.GetSecurityLogs(count);
            return records.Select(r => new LogEntry
            {
                Id = r.RecordId ?? 0,
                ProviderName = r.ProviderName,
                TimeCreated = r.TimeCreated,
                Message = r.FormatDescription()
            }).ToList();
        }
    }
}
