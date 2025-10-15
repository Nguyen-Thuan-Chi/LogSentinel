using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogSentinel.DAL.Data
{
    public class EventEntity
    {
        public int Id { get; set; }
        public DateTime Time { get; set; }
        public string Host { get; set; } = "";
        public string User { get; set; } = "";
        public string Level { get; set; } = ""; // Info/Warning/Error
        public string Process { get; set; } = "";
        public string Message { get; set; } = "";
        public string DetailsJson { get; set; } = "{}";
    }
}
