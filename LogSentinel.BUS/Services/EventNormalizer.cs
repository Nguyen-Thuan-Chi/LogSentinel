using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
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
                Source = "Sample",
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
                Source = "WindowsEventLog",
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

        public EventDto NormalizeFromSysmonEvent(EventRecord eventRecord)
        {
            var eventTime = eventRecord.TimeCreated ?? DateTime.UtcNow;
            var eventId = eventRecord.Id;
            var provider = eventRecord.ProviderName ?? "Sysmon";
            var level = MapEventLevel(eventRecord.Level);

            // Parse Sysmon-specific fields from XML
            var sysmonData = ParseSysmonEventData(eventRecord);

            var process = sysmonData.GetValueOrDefault("Image", "");
            var parentProcess = sysmonData.GetValueOrDefault("ParentImage", "");
            var commandLine = sysmonData.GetValueOrDefault("CommandLine", "");
            var user = sysmonData.GetValueOrDefault("User", eventRecord.UserId?.Value ?? "SYSTEM");
            var action = GetSysmonAction(eventId);
            var targetObject = GetSysmonObject(eventId, sysmonData);

            // Build comprehensive details JSON
            var detailsJson = JsonSerializer.Serialize(new
            {
                event_id = eventId,
                provider = provider,
                computer = eventRecord.MachineName,
                process_id = sysmonData.GetValueOrDefault("ProcessId", ""),
                process_guid = sysmonData.GetValueOrDefault("ProcessGuid", ""),
                parent_process_id = sysmonData.GetValueOrDefault("ParentProcessId", ""),
                parent_process_guid = sysmonData.GetValueOrDefault("ParentProcessGuid", ""),
                command_line = commandLine,
                current_directory = sysmonData.GetValueOrDefault("CurrentDirectory", ""),
                image_loaded = sysmonData.GetValueOrDefault("ImageLoaded", ""),
                target_filename = sysmonData.GetValueOrDefault("TargetFilename", ""),
                target_object = sysmonData.GetValueOrDefault("TargetObject", ""),
                registry_key = sysmonData.GetValueOrDefault("TargetObject", ""),
                source_ip = sysmonData.GetValueOrDefault("SourceIp", ""),
                destination_ip = sysmonData.GetValueOrDefault("DestinationIp", ""),
                source_port = sysmonData.GetValueOrDefault("SourcePort", ""),
                destination_port = sysmonData.GetValueOrDefault("DestinationPort", ""),
                protocol = sysmonData.GetValueOrDefault("Protocol", ""),
                hashes = sysmonData.GetValueOrDefault("Hashes", ""),
                integrity_level = sysmonData.GetValueOrDefault("IntegrityLevel", ""),
                logon_id = sysmonData.GetValueOrDefault("LogonId", ""),
                all_fields = sysmonData
            });

            return new EventDto
            {
                EventTime = eventTime,
                EventId = eventId,
                Provider = provider,
                Level = level,
                Host = eventRecord.MachineName ?? Environment.MachineName,
                User = user,
                Process = process,
                ParentProcess = parentProcess,
                Action = action,
                Object = targetObject,
                Message = eventRecord.FormatDescription() ?? action,
                Source = "Sysmon",
                DetailsJson = detailsJson,
                RawXml = eventRecord.ToXml()
            };
        }

        private Dictionary<string, string> ParseSysmonEventData(EventRecord eventRecord)
        {
            var result = new Dictionary<string, string>();

            try
            {
                var xml = XDocument.Parse(eventRecord.ToXml());
                var ns = xml.Root?.GetDefaultNamespace();
                
                if (ns != null)
                {
                    var eventData = xml.Descendants(ns + "EventData").FirstOrDefault();
                    if (eventData != null)
                    {
                        foreach (var data in eventData.Elements(ns + "Data"))
                        {
                            var name = data.Attribute("Name")?.Value;
                            var value = data.Value;
                            
                            if (!string.IsNullOrEmpty(name))
                            {
                                result[name] = value ?? "";
                            }
                        }
                    }
                }
            }
            catch
            {
                // If XML parsing fails, return empty dictionary
            }

            return result;
        }

        private string GetSysmonAction(int eventId)
        {
            return eventId switch
            {
                1 => "Process Create",
                2 => "File Creation Time Changed",
                3 => "Network Connection",
                5 => "Process Terminated",
                6 => "Driver Loaded",
                7 => "Image Loaded",
                8 => "CreateRemoteThread",
                9 => "RawAccessRead",
                10 => "Process Access",
                11 => "File Created",
                12 => "Registry Event (Object create/delete)",
                13 => "Registry Event (Value Set)",
                14 => "Registry Event (Key/Value Rename)",
                15 => "File Create Stream Hash",
                17 => "Pipe Created",
                18 => "Pipe Connected",
                19 => "WMI Event Filter",
                20 => "WMI Event Consumer",
                21 => "WMI Event Consumer To Filter",
                22 => "DNS Query",
                23 => "File Delete",
                24 => "Clipboard Change",
                25 => "Process Tampering",
                26 => "File Delete Detected",
                27 => "File Block Executable",
                28 => "File Block Shredding",
                29 => "File Executable Detected",
                _ => $"Sysmon Event {eventId}"
            };
        }

        private string GetSysmonObject(int eventId, Dictionary<string, string> data)
        {
            return eventId switch
            {
                1 => data.GetValueOrDefault("Image", ""), // Process
                3 => $"{data.GetValueOrDefault("DestinationIp", "")}:{data.GetValueOrDefault("DestinationPort", "")}", // Network
                7 => data.GetValueOrDefault("ImageLoaded", ""), // DLL
                11 => data.GetValueOrDefault("TargetFilename", ""), // File
                13 => data.GetValueOrDefault("TargetObject", ""), // Registry
                22 => data.GetValueOrDefault("QueryName", ""), // DNS
                23 => data.GetValueOrDefault("TargetFilename", ""), // File delete
                _ => data.GetValueOrDefault("TargetObject", 
                     data.GetValueOrDefault("TargetFilename", 
                     data.GetValueOrDefault("ImageLoaded", "")))
            };
        }
    }
}
