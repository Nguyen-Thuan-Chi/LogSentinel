using System;

namespace LogSentinel.BUS.Models
{
    public class EventDto
    {
        public DateTime EventTime { get; set; }
        public string Host { get; set; } = "";
        public string User { get; set; } = "";
        public int? EventId { get; set; }
        public string Provider { get; set; } = "";
        public string Level { get; set; } = "";
        public string Process { get; set; } = "";
        public string ParentProcess { get; set; } = "";
        public string Action { get; set; } = "";
        public string Object { get; set; } = "";
        public string Message { get; set; } = "";
        public string DetailsJson { get; set; } = "{}";
        public string? RawXml { get; set; }
    }
}
