using System;
using System.Diagnostics.Eventing.Reader;
using System.Text.Json;
using System.Text.RegularExpressions;
using LogSentinel.BUS.Interfaces;
using LogSentinel.BUS.Models;

namespace LogSentinel.BUS.Services
{
    public class EventNormalizer : IEventNormalizer
    {
        // Sample log format: 2024-01-15 14:30:45 [INFO] HOST1 alice svchost.exe Application started
        private static readonly Regex LogLineRegex = new(
            @"^(?<timestamp>\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2})\s+\[(?<level>\w+)\]\s+(?<host>\S+)\s+(?<user>\S+)\s+(?<process>\S+)\s+(?<message>.+)$",
            RegexOptions.Compiled);

        public EventDto Normalize(string rawLogLine)
        {
            var match = LogLineRegex.Match(rawLogLine);
            
            if (!match.Success)
            {
                // Fallback parsing
                return new EventDto
                {
                    EventTime = DateTime.UtcNow,
                    Level = "Info",
                    Message = rawLogLine,
                    Host = Environment.MachineName,
                    User = Environment.UserName,
                    Process = "unknown"
                };
            }

            var timestamp = DateTime.Parse(match.Groups["timestamp"].Value);
            var level = match.Groups["level"].Value;
            var host = match.Groups["host"].Value;
            var user = match.Groups["user"].Value;
            var process = match.Groups["process"].Value;
            var message = match.Groups["message"].Value;

            return new EventDto
            {
                EventTime = timestamp,
                Level = level,
                Host = host,
                User = user,
                Process = process,
                Message = message,
                Provider = "LogFile",
                DetailsJson = JsonSerializer.Serialize(new
                {
                    raw_line = rawLogLine,
                    parsed_at = DateTime.UtcNow
                })
            };
        }

        public EventDto NormalizeFromWindowsEvent(EventRecord eventRecord)
        {
            var eventTime = eventRecord.TimeCreated ?? DateTime.UtcNow;
            var level = MapEventLevel(eventRecord.Level);
            var eventId = eventRecord.Id;
            var provider = eventRecord.ProviderName ?? "Unknown";
            var message = eventRecord.FormatDescription() ?? "No description";

            var detailsJson = JsonSerializer.Serialize(new
            {
                event_id = eventId,
                provider = provider,
                computer = eventRecord.MachineName,
                process_id = eventRecord.ProcessId,
                thread_id = eventRecord.ThreadId,
                task_category = eventRecord.TaskDisplayName,
                keywords = eventRecord.KeywordsDisplayNames
            });

            return new EventDto
            {
                EventTime = eventTime,
                EventId = eventId,
                Provider = provider,
                Level = level,
                Host = eventRecord.MachineName ?? Environment.MachineName,
                User = eventRecord.UserId?.Value ?? "SYSTEM",
                Message = message,
                DetailsJson = detailsJson,
                RawXml = eventRecord.ToXml()
            };
        }

        private string MapEventLevel(byte? level)
        {
            return level switch
            {
                1 => "Critical",
                2 => "Error",
                3 => "Warning",
                4 => "Info",
                5 => "Verbose",
                _ => "Info"
            };
        }
    }
}
