#!/usr/bin/env pwsh

# Test script for Alert Detail Drill-Down Feature
Write-Host "=== Testing Alert Detail Drill-Down Feature ===" -ForegroundColor Cyan

# Create test alerts with detailed information
$tempProjectPath = "TestAlertDetailCreator"
$tempDir = Join-Path $PWD $tempProjectPath

if (Test-Path $tempDir) {
    Remove-Item $tempDir -Recurse -Force
}

Write-Host "Creating test project for alert details..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path $tempDir | Out-Null
Set-Location $tempDir

# Create console app
dotnet new console --force | Out-Null

# Add references
dotnet add reference "..\LogSentinel.BUS\LogSentinel.BUS.csproj" | Out-Null
dotnet add reference "..\LogSentinel.DAL\LogSentinel.DAL.csproj" | Out-Null
dotnet add package Microsoft.EntityFrameworkCore.Sqlite | Out-Null
dotnet add package Microsoft.Extensions.DependencyInjection | Out-Null
dotnet add package Microsoft.Extensions.Logging | Out-Null
dotnet add package Microsoft.Extensions.Logging.Console | Out-Null

$programCode = @'
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LogSentinel.DAL.Data;
using LogSentinel.DAL.Repositories;
using LogSentinel.BUS.Services;
using LogSentinel.BUS.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LogSentinel");
Directory.CreateDirectory(appDataDir);
var dbPath = Path.Combine(appDataDir, "logsentinel.db");
var connectionString = $"Data Source={dbPath}";

Console.WriteLine($"Using database: {dbPath}");
services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

services.AddScoped<IAlertRepository, AlertRepository>();
services.AddScoped<IRuleRepository, RuleRepository>();
services.AddScoped<IAlertService, AlertService>();
services.AddScoped<IEventRepository, EventRepository>();

var serviceProvider = services.BuildServiceProvider();

using (var scope = serviceProvider.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var alertRepository = scope.ServiceProvider.GetRequiredService<IAlertRepository>();
    var ruleRepository = scope.ServiceProvider.GetRequiredService<IRuleRepository>();
    var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();
    var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
    
    try
    {
        await dbContext.Database.EnsureCreatedAsync();
        Console.WriteLine("? Database connection verified.");
        
        // Clean up old test data
        var existingAlerts = await alertRepository.GetAllAsync();
        foreach (var alert in existingAlerts.Where(a => a.Title.Contains("Drill-Down Test")))
        {
            await alertRepository.DeleteAsync(alert);
        }
        
        // Create test rule if needed
        var rules = await ruleRepository.GetAllAsync();
        var testRule = rules.FirstOrDefault(r => r.Name == "Security Breach Detection");
        
        if (testRule == null)
        {
            testRule = new RuleEntity
            {
                Name = "Security Breach Detection",
                Description = "Detects potential security breaches based on failed login patterns",
                Severity = "Critical",
                YamlContent = @"name: Security Breach Detection
description: Detects potential security breaches
severity: Critical
selection:
  event_id: [4625, 4648, 4771]
condition:
  count: 5
  timeframe: 300",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                TriggerCount = 0
            };
            
            await ruleRepository.AddAsync(testRule);
            Console.WriteLine($"? Created test rule: {testRule.Name}");
        }
        
        // Create detailed test events
        var triggeringEvents = new List<EventEntity>();
        
        // Event 1: Failed login attempt
        var event1 = new EventEntity
        {
            EventId = 4625,
            EventTime = DateTime.UtcNow.AddMinutes(-5),
            Level = "Warning",
            Provider = "Microsoft-Windows-Security-Auditing",
            Host = "DC-01.contoso.com",
            User = "john.doe",
            Process = "winlogon.exe",
            ParentProcess = "smss.exe",
            Action = "Logon Failure",
            Object = "Domain Controller",
            DetailsJson = JsonSerializer.Serialize(new
            {
                event_id = 4625,
                failure_reason = "Unknown user name or bad password",
                source_ip = "192.168.1.100",
                workstation_name = "DESKTOP-ABC123",
                logon_type = 3,
                authentication_package = "NTLM",
                process_name = "winlogon.exe",
                caller_process_id = "0x123",
                caller_process_name = "winlogon.exe"
            }),
            RawXml = @"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'>
  <System>
    <Provider Name='Microsoft-Windows-Security-Auditing' Guid='{54849625-5478-4994-a5ba-3e3b0328c30d}'/>
    <EventID>4625</EventID>
    <Version>0</Version>
    <Level>0</Level>
    <Task>12544</Task>
    <Opcode>0</Opcode>
    <Keywords>0x8010000000000000</Keywords>
    <TimeCreated SystemTime='2025-01-19T15:30:45.123456Z'/>
    <EventRecordID>12345</EventRecordID>
  </System>
</Event>",
            Source = "WindowsEventLog",
            CreatedAt = DateTime.UtcNow
        };
        await eventRepository.AddAsync(event1);
        triggeringEvents.Add(event1);
        
        // Event 2: Another failed login
        var event2 = new EventEntity
        {
            EventId = 4625,
            EventTime = DateTime.UtcNow.AddMinutes(-3),
            Level = "Warning", 
            Provider = "Microsoft-Windows-Security-Auditing",
            Host = "DC-01.contoso.com",
            User = "admin",
            Process = "winlogon.exe",
            ParentProcess = "smss.exe",
            Action = "Logon Failure",
            Object = "Administrator Account",
            DetailsJson = JsonSerializer.Serialize(new
            {
                event_id = 4625,
                failure_reason = "Account locked out",
                source_ip = "192.168.1.100",
                workstation_name = "DESKTOP-ABC123",
                logon_type = 3,
                authentication_package = "NTLM",
                process_name = "winlogon.exe",
                caller_process_id = "0x456"
            }),
            Source = "WindowsEventLog",
            CreatedAt = DateTime.UtcNow
        };
        await eventRepository.AddAsync(event2);
        triggeringEvents.Add(event2);
        
        // Event 3: Suspicious PowerShell execution
        var event3 = new EventEntity
        {
            EventId = 4688,
            EventTime = DateTime.UtcNow.AddMinutes(-1),
            Level = "Information",
            Provider = "Microsoft-Windows-Security-Auditing", 
            Host = "WORKSTATION-01",
            User = "john.doe",
            Process = "powershell.exe",
            ParentProcess = "cmd.exe",
            Action = "Process Creation",
            Object = "C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe",
            DetailsJson = JsonSerializer.Serialize(new
            {
                event_id = 4688,
                command_line = "powershell.exe -ExecutionPolicy Bypass -WindowStyle Hidden -EncodedCommand SQBuAHYAbwBrAGUALQBXAGUAYgBSAGUAcQB1AGUAcwB0AA==",
                process_id = "0x789",
                parent_process_id = "0x123",
                creator_process_id = "0x456",
                mandatory_label = "Medium Mandatory Level",
                token_elevation_type = "TokenElevationTypeDefault"
            }),
            Source = "WindowsEventLog",
            CreatedAt = DateTime.UtcNow
        };
        await eventRepository.AddAsync(event3);
        triggeringEvents.Add(event3);
        
        Console.WriteLine($"? Created {triggeringEvents.Count} detailed triggering events");
        
        // Create comprehensive test alert
        var alertTitle = $"Security Breach Alert - Drill-Down Test - {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        var alertDescription = @"CRITICAL SECURITY ALERT: Multiple failed login attempts detected followed by suspicious PowerShell execution.

SUMMARY:
- 2 failed login attempts for users 'john.doe' and 'admin' 
- 1 suspicious PowerShell process with encoded command execution
- All events originated from IP 192.168.1.100 within 5-minute window
- Potential credential stuffing or brute force attack in progress

RECOMMENDED ACTIONS:
1. Block source IP 192.168.1.100 immediately
2. Force password reset for affected accounts
3. Review PowerShell execution policies
4. Check for lateral movement indicators
5. Escalate to SOC team for further investigation

This alert was created by the LogSentinel drill-down test script.";

        var alert = await alertService.CreateAlertAsync(testRule, triggeringEvents, alertTitle, alertDescription);
        
        Console.WriteLine("? DETAILED TEST ALERT CREATED SUCCESSFULLY!");
        Console.WriteLine($"  Alert ID: {alert.Id}");
        Console.WriteLine($"  Title: {alert.Title}");
        Console.WriteLine($"  Severity: {alert.Severity} (should show as Critical)");
        Console.WriteLine($"  Rule: {alert.RuleName}");
        Console.WriteLine($"  Timestamp: {alert.Timestamp}");
        Console.WriteLine($"  Description: {alert.Description.Substring(0, Math.Min(100, alert.Description.Length))}...");
        Console.WriteLine($"  Triggering Events: {triggeringEvents.Count}");
        
        // Verify data integrity
        var savedAlert = await alertRepository.GetByIdAsync(alert.Id);
        if (savedAlert != null)
        {
            Console.WriteLine("? Alert saved to database successfully");
            
            // Check EventIdsJson
            var eventIds = JsonSerializer.Deserialize<long[]>(savedAlert.EventIdsJson);
            Console.WriteLine($"? Event IDs stored: [{string.Join(", ", eventIds)}]");
            
            // Verify first triggering event can be retrieved
            var firstEvent = await eventRepository.GetByIdAsync(eventIds[0]);
            if (firstEvent != null)
            {
                Console.WriteLine($"? First triggering event retrievable: ID {firstEvent.Id}, EventID {firstEvent.EventId}");
                Console.WriteLine($"  Event details preview: {firstEvent.DetailsJson.Substring(0, Math.Min(80, firstEvent.DetailsJson.Length))}...");
            }
        }
        
        // Count total data
        var allAlerts = await alertRepository.GetAllAsync();
        var allEvents = await eventRepository.GetAllAsync(); 
        Console.WriteLine($"\nDATABASE SUMMARY:");
        Console.WriteLine($"  Total alerts: {allAlerts.Count()}");
        Console.WriteLine($"  Total events: {allEvents.Count()}");
        
    }
    catch (Exception ex)
    {
        Console.WriteLine($"? Error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
}

Console.WriteLine("\n=== DRILL-DOWN TEST INSTRUCTIONS ===");
Console.WriteLine("1. Start the LogSentinel application");
Console.WriteLine("2. Navigate to the Alerts view");
Console.WriteLine("3. Find the 'Security Breach Alert - Drill-Down Test' alert");
Console.WriteLine("4. DOUBLE-CLICK the alert row to open the detail window");
Console.WriteLine("5. Verify all three information sections display:");
Console.WriteLine("   - Alert Information (ID, Rule, Severity, etc.)");
Console.WriteLine("   - Triggering Event Information (Host, User, Process, etc.)");
Console.WriteLine("   - Event Details (formatted JSON)");
Console.WriteLine("6. Check that the data is properly formatted and readable");
Console.WriteLine("7. Close the detail window and test with other alerts");
Console.WriteLine("\n? The drill-down feature is now ready for testing!");
'@

$programCode | Out-File -FilePath "Program.cs" -Encoding UTF8

Write-Host "Building and running detailed alert creator..." -ForegroundColor Yellow
$buildResult = dotnet build --verbosity quiet 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Build successful" -ForegroundColor Green
    dotnet run
} else {
    Write-Host "? Build failed:" -ForegroundColor Red
    Write-Host $buildResult -ForegroundColor Red
}

# Cleanup
Set-Location ..
Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "`n=== TESTING CHECKLIST ===" -ForegroundColor Cyan
Write-Host "Å† Alert appears in AlertsView DataGrid" -ForegroundColor Yellow
Write-Host "Å† Double-clicking opens AlertDetailView window" -ForegroundColor Yellow
Write-Host "Å† Alert Information section displays correctly" -ForegroundColor Yellow
Write-Host "Å† Triggering Event Information loads and displays" -ForegroundColor Yellow
Write-Host "Å† Event Details JSON is properly formatted" -ForegroundColor Yellow
Write-Host "Å† Window closes properly with Close button" -ForegroundColor Yellow
Write-Host "Å† Can open details for multiple different alerts" -ForegroundColor Yellow
Write-Host "Å† Loading indicator shows/hides appropriately" -ForegroundColor Yellow