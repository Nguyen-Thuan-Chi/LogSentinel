using LogSentinel.DAL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Eventing.Reader;

namespace LogSentinel.DAL.Data
{
    public class LogRepository : ILogRepository
    {
        public List<EventRecord> GetSecurityLogs(int maxRecords = 20)
        {
            var logs = new List<EventRecord>();
            var query = new EventLogQuery("Security", PathType.LogName);
            using var reader = new EventLogReader(query);

            int count = 0;
            EventRecord record;
            while ((record = reader.ReadEvent()) != null && count < maxRecords)
            {
                logs.Add(record);
                count++;
            }
            return logs;
        }
    }
}
