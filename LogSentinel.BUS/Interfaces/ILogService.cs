using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogSentinel.BUS.Models;

namespace LogSentinel.BUS.Interfaces
{
    public interface ILogService
    {
        List<LogEntry> GetRecentLogs(int count = 20);
    }
}
