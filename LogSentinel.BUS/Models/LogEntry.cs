using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogSentinel.BUS.Models
{
    public class LogEntry
    {
        public long Id { get; set; }
        public string ProviderName { get; set; }
        public DateTime? TimeCreated { get; set; }
        public string Message { get; set; }
    }
}
