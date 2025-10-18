using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace LogSentinel.DAL.Data
{
    public static class SeedData
    {
        private static readonly Random _random = new();
        
        private static readonly string[] _hosts = { "WEB-SERVER-01", "DB-SERVER-01", "APP-SERVER-01", "DC-01", "WORKSTATION-42" };
        private static readonly string[] _users = { "alice", "bob", "charlie", "dave", "eve", "admin", "service_account", "guest" };
        private static readonly string[] _processes = { "svchost.exe", "chrome.exe", "powershell.exe", "cmd.exe", "explorer.exe", "sqlservr.exe", "w3wp.exe", "lsass.exe" };
        private static readonly string[] _levels = { "Info", "Info", "Info", "Warning", "Error" }; // Info is more common
        private static readonly string[] _providers = { "Microsoft-Windows-Security-Auditing", "Microsoft-Windows-PowerShell", "System", "Application" };
        
        private static readonly string[] _messages = {
            "Application started successfully",
            "User logged in",
            "File access granted",
            "Service started",
            "Configuration updated",
            "Memory usage is high",
            "Disk space running low",
            "Connection timeout",
            "Failed to connect to database",
            "Authentication failed",
            "Access denied",
            "Service crashed unexpectedly"
        };

        public static async Task SeedDatabaseAsync(AppDbContext context)
        {
            // Only seed Rules - Real events will come from Windows Event Log and Sysmon
            if (!await context.Rules.AnyAsync())
            {
                await SeedRulesAsync(context);
            }

            // Do not seed fake events - use real-time data instead
            // Real events will be imported from:
            // 1. Windows Event Log (Security, System, Application)
            // 2. Sysmon events
            // 3. Sample log files (if enabled)
        }

        private static async Task SeedRulesAsync(AppDbContext context)
        {
            var rulesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Rules");
            
            if (!Directory.Exists(rulesPath))
            {
                // Create rules in database directly if files don't exist
                var rules = new List<RuleEntity>
                {
                    new RuleEntity
                    {
                        Name = "Failed Login Threshold",
                        Description = "Detects multiple failed login attempts",
                        Severity = "High",
                        IsEnabled = true,
                        YamlContent = @"name: Failed Login Threshold
description: Detects multiple failed login attempts (Event ID 4625) from the same user
severity: High
enabled: true
selection:
  event_id: 4625
condition:
  count: 5
  timeframe: 300
  group_by: user"
                    },
                    new RuleEntity
                    {
                        Name = "Admin User Created",
                        Description = "Detects when a user is added to the Administrators group",
                        Severity = "Critical",
                        IsEnabled = true,
                        YamlContent = @"name: Admin User Created
description: Detects when a user is added to the Administrators group
severity: Critical
enabled: true
selection:
  event_id: 4732
condition:
  pattern: ""(?i)(administrators|admin)"""
                    },
                    new RuleEntity
                    {
                        Name = "Suspicious PowerShell Execution",
                        Description = "Detects PowerShell with suspicious flags",
                        Severity = "High",
                        IsEnabled = true,
                        YamlContent = @"name: Suspicious PowerShell Execution
description: Detects PowerShell with suspicious command-line flags
severity: High
enabled: true
selection:
  process: ""powershell.exe""
condition:
  pattern: ""(?i)(-enc|-nop|-w hidden)"""
                    },
                    new RuleEntity
                    {
                        Name = "Process Creation Monitoring",
                        Description = "Monitors new process creation from Sysmon Event ID 1",
                        Severity = "Medium",
                        IsEnabled = true,
                        YamlContent = @"name: Process Creation Monitoring
description: Monitors new process creation from Sysmon Event ID 1
severity: Medium
enabled: true
selection:
  event_id: 1
  source: Sysmon
condition:
  pattern: ""(?i)(cmd|powershell|wscript|cscript)"""
                    },
                    new RuleEntity
                    {
                        Name = "Network Connection Monitoring", 
                        Description = "Monitors network connections from Sysmon Event ID 3",
                        Severity = "Low",
                        IsEnabled = true,
                        YamlContent = @"name: Network Connection Monitoring
description: Monitors network connections from Sysmon Event ID 3
severity: Low
enabled: true
selection:
  event_id: 3
  source: Sysmon
condition:
  pattern: ""(?i)(443|80|22|21|23)"""
                    }
                };

                await context.Rules.AddRangeAsync(rules);
            }
            else
            {
                // Load from YAML files
                var yamlFiles = Directory.GetFiles(rulesPath, "*.yaml");
                foreach (var file in yamlFiles)
                {
                    var yamlContent = await File.ReadAllTextAsync(file);
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    
                    // Simple YAML parsing (in production, use YamlDotNet)
                    var name = ExtractYamlValue(yamlContent, "name") ?? fileName;
                    var description = ExtractYamlValue(yamlContent, "description") ?? "";
                    var severity = ExtractYamlValue(yamlContent, "severity") ?? "Medium";
                    var enabled = ExtractYamlValue(yamlContent, "enabled")?.ToLower() == "true";

                    var rule = new RuleEntity
                    {
                        Name = name,
                        Description = description,
                        Severity = severity,
                        IsEnabled = enabled,
                        YamlContent = yamlContent
                    };

                    await context.Rules.AddAsync(rule);
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedEventsAsync(AppDbContext context, int count = 500)
        {
            var events = new List<EventEntity>();
            var now = DateTime.UtcNow;

            for (int i = 0; i < count; i++)
            {
                var eventTime = now.AddMinutes(-_random.Next(0, 10080)); // Last 7 days
                var host = _hosts[_random.Next(_hosts.Length)];
                var user = _users[_random.Next(_users.Length)];
                var process = _processes[_random.Next(_processes.Length)];
                var level = _levels[_random.Next(_levels.Length)];
                var provider = _providers[_random.Next(_providers.Length)];
                var message = _messages[_random.Next(_messages.Length)];
                
                var eventId = _random.Next(1000, 5000);
                
                // Generate some failed logins for testing
                if (i % 50 == 0)
                {
                    eventId = 4625; // Failed login
                    level = "Warning";
                    message = "An account failed to log on";
                }

                // Generate some admin events
                if (i % 100 == 0)
                {
                    eventId = 4732; // Member added to security-enabled local group
                    level = "Info";
                    message = "A member was added to a security-enabled local group";
                }

                var detailsJson = JsonSerializer.Serialize(new
                {
                    event_id = eventId,
                    provider = provider,
                    process_id = _random.Next(1000, 9999),
                    thread_id = _random.Next(100, 999),
                    computer = host
                });

                var evt = new EventEntity
                {
                    EventTime = eventTime,
                    Host = host,
                    User = user,
                    EventId = eventId,
                    Provider = provider,
                    Level = level,
                    Process = process,
                    ParentProcess = "services.exe",
                    Action = "Logon",
                    Object = $"C:\\Windows\\System32\\{process}",
                    DetailsJson = detailsJson,
                    RawXml = $"<Event><System><EventID>{eventId}</EventID></System></Event>",
                    CreatedAt = DateTime.UtcNow
                };

                events.Add(evt);
            }

            await context.Events.AddRangeAsync(events);
            await context.SaveChangesAsync();
        }

        private static string? ExtractYamlValue(string yaml, string key)
        {
            var lines = yaml.Split('\n');
            foreach (var line in lines)
            {
                if (line.TrimStart().StartsWith($"{key}:"))
                {
                    var value = line.Substring(line.IndexOf(':') + 1).Trim();
                    return value.Trim('"', '\'');
                }
            }
            return null;
        }
    }
}
