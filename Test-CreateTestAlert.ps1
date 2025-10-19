#!/usr/bin/env pwsh

# Test Alert Creation Script
# Creates a test alert using the LogSentinel API directly

Write-Host "=== LogSentinel Test Alert Creation ===" -ForegroundColor Cyan

# Build and run a temporary console app to create test alerts
$tempProjectPath = "TestAlertCreator"
$tempDir = Join-Path $PWD $tempProjectPath

if (Test-Path $tempDir) {
    Remove-Item $tempDir -Recurse -Force
}

Write-Host "Creating temporary test project..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path $tempDir | Out-Null
Set-Location $tempDir

# Create a simple console app
dotnet new console --force | Out-Null

# Add references to LogSentinel projects
dotnet add reference "..\LogSentinel.BUS\LogSentinel.BUS.csproj" | Out-Null
dotnet add reference "..\LogSentinel.DAL\LogSentinel.DAL.csproj" | Out-Null

# Add required packages
dotnet add package Microsoft.EntityFrameworkCore.Sqlite | Out-Null
dotnet add package Microsoft.Extensions.DependencyInjection | Out-Null
dotnet add package Microsoft.Extensions.Logging | Out-Null
dotnet add package Microsoft.Extensions.Logging.Console | Out-Null

# Create Program.cs
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

var services = new ServiceCollection();

// Configure logging
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

// Configure database
var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LogSentinel");
Directory.CreateDirectory(appDataDir);
var dbPath = Path.Combine(appDataDir, "logsentinel.db");
var connectionString = $"Data Source={dbPath}";

Console.WriteLine($"Using database: {dbPath}");

services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

// Register repositories and services
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
        // Ensure database exists
        await dbContext.Database.EnsureCreatedAsync();
        Console.WriteLine("Database connection verified.");
        
        // Check for existing rules
        var rules = await ruleRepository.GetAllAsync();
        Console.WriteLine($"Found {rules.Count()} rules in database");
        
        var activeRules = rules.Where(r => r.IsEnabled).ToList();
        Console.WriteLine($"Found {activeRules.Count} active rules");
        
        if (!activeRules.Any())
        {
            Console.WriteLine("No active rules found. Creating a test rule...");
            
            var testRule = new RuleEntity
            {
                Name = "Test Alert Rule",
                Description = "A test rule for generating alerts",
                Severity = "High",
                YamlContent = "test: true",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                TriggerCount = 0
            };
            
            await ruleRepository.AddAsync(testRule);
            Console.WriteLine($"Created test rule with ID: {testRule.Id}");
            activeRules.Add(testRule);
        }
        
        // Create test events for the alert
        var testEvents = new List<EventEntity>();
        for (int i = 0; i < 3; i++)
        {
            var testEvent = new EventEntity
            {
                EventId = 4624 + i,
                EventTime = DateTime.UtcNow.AddMinutes(-i),
                Level = "Information",
                Provider = "Microsoft-Windows-Security-Auditing",
                Host = Environment.MachineName,
                User = Environment.UserName,
                Process = "TestProcess.exe",
                Action = "TestAction",
                DetailsJson = $"{{\"test\": true, \"index\": {i}}}"
            };
            
            await eventRepository.AddAsync(testEvent);
            testEvents.Add(testEvent);
        }
        
        Console.WriteLine($"Created {testEvents.Count} test events");
        
        // Create test alert
        var rule = activeRules.First();
        var alertTitle = $"Test Alert - {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        var alertDescription = "This is a test alert created by the PowerShell script to verify alert display functionality.";
        
        var alert = await alertService.CreateAlertAsync(rule, testEvents, alertTitle, alertDescription);
        
        Console.WriteLine($"? Test alert created successfully!");
        Console.WriteLine($"  Alert ID: {alert.Id}");
        Console.WriteLine($"  Title: {alert.Title}");
        Console.WriteLine($"  Severity: {alert.Severity}");
        Console.WriteLine($"  Rule: {alert.RuleName}");
        Console.WriteLine($"  Timestamp: {alert.Timestamp}");
        
        // Verify alert was saved
        var savedAlert = await alertRepository.GetByIdAsync(alert.Id);
        if (savedAlert != null)
        {
            Console.WriteLine("? Alert was successfully saved to database");
        }
        else
        {
            Console.WriteLine("? Alert was not saved to database");
        }
        
        // Count total alerts
        var allAlerts = await alertRepository.GetAllAsync();
        Console.WriteLine($"Total alerts in database: {allAlerts.Count()}");
        
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
}

Console.WriteLine("\n=== Test Complete ===");
Console.WriteLine("Now start the LogSentinel application and check the Alerts view.");
'@

$programCode | Out-File -FilePath "Program.cs" -Encoding UTF8

Write-Host "Building test application..." -ForegroundColor Yellow
$buildResult = dotnet build --verbosity quiet 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Build successful" -ForegroundColor Green
    Write-Host "Running test alert creator..." -ForegroundColor Yellow
    dotnet run
} else {
    Write-Host "? Build failed:" -ForegroundColor Red
    Write-Host $buildResult -ForegroundColor Red
}

# Clean up
Set-Location ..
Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "`n=== Next Steps ===" -ForegroundColor Cyan
Write-Host "1. Start the LogSentinel application" -ForegroundColor White
Write-Host "2. Navigate to the Alerts view" -ForegroundColor White
Write-Host "3. Check if the test alert appears" -ForegroundColor White
Write-Host "4. If it doesn't appear, check the debug output in Visual Studio" -ForegroundColor White