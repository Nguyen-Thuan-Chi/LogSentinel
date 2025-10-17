using System;

namespace LogSentinel.BUS.Models
{
    public class AlertDto
    {
        public long Id { get; set; }
        public string RuleName { get; set; } = "";
        public string Severity { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsAcknowledged { get; set; }
    }
}
