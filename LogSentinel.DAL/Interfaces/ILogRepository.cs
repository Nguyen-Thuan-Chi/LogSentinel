using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Eventing.Reader;

namespace LogSentinel.DAL.Interfaces
{
    public interface ILogRepository
    {
        List<EventRecord> GetSecurityLogs(int maxRecords = 20);
    }
}
