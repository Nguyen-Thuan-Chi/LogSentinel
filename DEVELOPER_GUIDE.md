# LogSentinel - Developer Quick Reference

## ?? Quick Commands

```bash
# Build
dotnet build

# Run
cd "Log Sentinel"
dotnet run

# Test
dotnet test

# Clean
dotnet clean

# Restore packages
dotnet restore

# Generate migration
cd LogSentinel.DAL
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove
```

## ?? Project Structure

```
Log Sentinel/
Ñ•ÑüÑü Log Sentinel/                    # UI Layer (WPF)
Ñ†   Ñ•ÑüÑü App.xaml.cs                  # DI container setup
Ñ†   Ñ•ÑüÑü UI/
Ñ†   Ñ†   Ñ•ÑüÑü MainWindow.xaml          # Main shell
Ñ†   Ñ†   Ñ•ÑüÑü EventsView.xaml          # Events grid
Ñ†   Ñ†   Ñ§ÑüÑü [other views]
Ñ†   Ñ•ÑüÑü ViewModels/
Ñ†   Ñ†   Ñ•ÑüÑü MainViewModel.cs         # Navigation
Ñ†   Ñ†   Ñ§ÑüÑü EventsViewModel.cs       # Event data binding
Ñ†   Ñ§ÑüÑü appsettings.json             # Configuration
Ñ†
Ñ•ÑüÑü LogSentinel.BUS/                 # Business Logic
Ñ†   Ñ•ÑüÑü Interfaces/
Ñ†   Ñ†   Ñ•ÑüÑü IEventNormalizer.cs
Ñ†   Ñ†   Ñ•ÑüÑü IRuleEngine.cs
Ñ†   Ñ†   Ñ•ÑüÑü IAlertService.cs
Ñ†   Ñ†   Ñ§ÑüÑü IEventImporter.cs
Ñ†   Ñ•ÑüÑü Services/
Ñ†   Ñ†   Ñ•ÑüÑü EventNormalizer.cs       # Log parsing
Ñ†   Ñ†   Ñ•ÑüÑü RuleEngine.cs            # Detection logic
Ñ†   Ñ†   Ñ•ÑüÑü AlertService.cs          # Alert management
Ñ†   Ñ†   Ñ§ÑüÑü EventImporter.cs         # File watching
Ñ†   Ñ§ÑüÑü Models/
Ñ†       Ñ•ÑüÑü EventDto.cs
Ñ†       Ñ•ÑüÑü AlertDto.cs
Ñ†       Ñ§ÑüÑü RuleDefinition.cs
Ñ†
Ñ•ÑüÑü LogSentinel.DAL/                 # Data Access
Ñ†   Ñ•ÑüÑü Data/
Ñ†   Ñ†   Ñ•ÑüÑü AppDbContext.cs          # EF Context
Ñ†   Ñ†   Ñ•ÑüÑü EventEntity.cs           # Event model
Ñ†   Ñ†   Ñ•ÑüÑü AlertEntity.cs           # Alert model
Ñ†   Ñ†   Ñ•ÑüÑü RuleEntity.cs            # Rule model
Ñ†   Ñ†   Ñ•ÑüÑü SeedData.cs              # Database seeding
Ñ†   Ñ†   Ñ§ÑüÑü Rules/*.yaml             # Rule definitions
Ñ†   Ñ•ÑüÑü Repositories/
Ñ†   Ñ†   Ñ•ÑüÑü EFRepository.cs          # Base repo
Ñ†   Ñ†   Ñ•ÑüÑü EventRepository.cs       # Event queries
Ñ†   Ñ†   Ñ•ÑüÑü AlertRepository.cs       # Alert queries
Ñ†   Ñ†   Ñ§ÑüÑü RuleRepository.cs        # Rule queries
Ñ†   Ñ§ÑüÑü Migrations/
Ñ†
Ñ§ÑüÑü LogSentinel.Tests/               # Unit Tests
    Ñ•ÑüÑü EventNormalizerTests.cs
    Ñ•ÑüÑü RuleEngineTests.cs
    Ñ§ÑüÑü AlertServiceTests.cs
```

## ?? Dependency Injection Map

**Registered in `App.xaml.cs` Å® `OnStartup()`:**

| Interface | Implementation | Lifetime |
|-----------|----------------|----------|
| `IConfiguration` | Configuration from JSON | Singleton |
| `AppDbContext` | EF DbContext | Scoped |
| `IEventRepository` | EventRepository | Scoped |
| `IAlertRepository` | AlertRepository | Scoped |
| `IRuleRepository` | RuleRepository | Scoped |
| `IEventNormalizer` | EventNormalizer | Singleton |
| `IRuleProvider` | RuleProvider | Scoped |
| `IRuleEngine` | RuleEngine | Scoped |
| `IAlertService` | AlertService | Scoped |
| `IEventImporter` | EventImporter | Scoped |
| `MainViewModel` | MainViewModel | Singleton |
| `EventsViewModel` | EventsViewModel | Singleton |
| `MainWindow` | MainWindow | Singleton |

## ?? Key Classes

### EventNormalizer
```csharp
// Parse log line to EventDto
var dto = _normalizer.Normalize(logLine);

// Parse Windows Event to EventDto
var dto = _normalizer.NormalizeFromWindowsEvent(eventRecord);
```

### RuleEngine
```csharp
// Initialize and load rules
await _ruleEngine.InitializeAsync();

// Evaluate single event
bool triggered = await _ruleEngine.EvaluateEventAsync(eventEntity);

// Batch evaluation
var matches = await _ruleEngine.EvaluateBatchAsync(startTime, endTime);

// Reload rules from database
await _ruleEngine.ReloadRulesAsync();
```

### AlertService
```csharp
// Create alert
var alert = await _alertService.CreateAlertAsync(
    rule, matchingEvents, "Title", "Description");

// Get recent alerts
var alerts = await _alertService.GetRecentAlertsAsync(minutes: 5);

// Acknowledge
await _alertService.AcknowledgeAlertAsync(alertId, "admin");

// Export
await _alertService.ExportToJsonAsync("alerts.json");
await _alertService.ExportToCsvAsync("alerts.csv");

// Webhook
await _alertService.SendWebhookAsync(alert, webhookUrl);
```

### EventImporter
```csharp
// Start streaming (FileWatcher + Channel)
await _importer.StartStreamingAsync(cancellationToken);

// Batch import directory
await _importer.ImportBatchAsync("path/to/logs");
```

## ?? Database Schema

### Events Table
```sql
CREATE TABLE Events (
    Id BIGINT PRIMARY KEY,
    EventTime DATETIME NOT NULL,
    Host VARCHAR(256),
    User VARCHAR(256),
    EventId INT,
    Provider VARCHAR(256),
    Level VARCHAR(50),
    Process VARCHAR(512),
    ParentProcess VARCHAR(512),
    Action VARCHAR(256),
    Object VARCHAR(512),
    DetailsJson TEXT,
    RawXml TEXT,
    CreatedAt DATETIME
);

CREATE INDEX IX_Events_EventTime ON Events(EventTime);
CREATE INDEX IX_Events_EventId ON Events(EventId);
CREATE INDEX IX_Events_User ON Events(User);
CREATE INDEX IX_Events_Process ON Events(Process);
CREATE INDEX IX_Events_Host ON Events(Host);
```

### FTS5 Virtual Table
```sql
CREATE VIRTUAL TABLE EventsFTS USING fts5(
    Id UNINDEXED,
    Host, User, Process, Action, Object, DetailsJson, RawXml,
    content=Events,
    content_rowid=Id
);
```

## ?? Common Queries

### Get Recent Events
```csharp
var events = await _eventRepo.GetByDateRangeAsync(
    DateTime.Today, DateTime.Now);
```

### Search Events (FTS)
```csharp
var results = await _eventRepo.SearchAsync("powershell", skip: 0, take: 100);
```

### Filter Events
```csharp
var filtered = await _eventRepo.GetByFilterAsync(
    host: "DC-01",
    user: "admin",
    eventId: 4625,
    level: "Error",
    startDate: DateTime.Today,
    endDate: DateTime.Now,
    skip: 0,
    take: 50
);
```

### Get Today's Count
```csharp
int count = await _eventRepo.CountTodayAsync();
```

### Get Unacknowledged Alerts
```csharp
var alerts = await _alertRepo.GetUnacknowledgedAsync();
```

## ?? Rule YAML Format

```yaml
name: Rule Name
description: What this rule detects
severity: Low | Medium | High | Critical
enabled: true

selection:
  event_id: 4625                    # Optional: filter by event ID
  level: Error                      # Optional: filter by level
  provider: Security-Auditing       # Optional: filter by provider
  process: powershell.exe           # Optional: filter by process
  user: admin                       # Optional: filter by user
  host: DC-01                       # Optional: filter by host

condition:
  # Threshold-based detection
  count: 5                          # Trigger after N events
  timeframe: 300                    # Within N seconds
  group_by: user                    # Group by: user, host, process

  # Pattern-based detection
  pattern: "(?i)suspicious"         # Regex pattern
  field: details_json               # Field to search: details_json, action, process

  # Immediate trigger
  always: true                      # Trigger on every match

action:
  alert: true
  title: "Alert Title with {placeholders}"
  description: "Alert description with {user}, {host}, {count}"
```

**Placeholders:** `{user}`, `{host}`, `{count}`, `{process}`

## ?? Writing Tests

### Example Test
```csharp
[Fact]
public async Task TestName_Scenario_ExpectedResult()
{
    // Arrange
    var mock = new Mock<IDependency>();
    mock.Setup(x => x.Method()).ReturnsAsync(value);
    var sut = new SystemUnderTest(mock.Object);

    // Act
    var result = await sut.DoSomething();

    // Assert
    Assert.NotNull(result);
    Assert.Equal(expected, result.Property);
    mock.Verify(x => x.Method(), Times.Once);
}
```

## ?? Debugging Tips

### Enable Verbose Logging
```json
// appsettings.Development.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  }
}
```

### Check Logs
```powershell
Get-Content "Log Sentinel\logs\logsentinel-*.log" -Tail 50 -Wait
```

### Database Inspection (SQLite)
```powershell
# Install SQLite browser or use CLI
sqlite3 "Log Sentinel\data\logsentinel.db"

# Queries
SELECT COUNT(*) FROM Events;
SELECT COUNT(*) FROM Alerts;
SELECT * FROM Rules WHERE IsEnabled = 1;
```

### Performance Profiling
```csharp
var stopwatch = Stopwatch.StartNew();
// ... code to profile
stopwatch.Stop();
_logger.LogInformation("Operation took {Ms}ms", stopwatch.ElapsedMilliseconds);
```

## ?? Security Notes

- **SQL Injection**: All queries use parameterized EF queries ?
- **XSS**: XAML doesn't render HTML (not applicable) ?
- **Secrets**: Use User Secrets or env vars for sensitive config ?
- **Connection Strings**: Never commit real prod connection strings ?

## ?? Common Issues

**Issue:** Database locked  
**Fix:** Close all app instances, delete `.db-shm` and `.db-wal` files

**Issue:** Events not importing  
**Fix:** Check `EnableFileWatcher: true`, verify directory exists

**Issue:** Rules not triggering  
**Fix:** Check rule `enabled: true`, verify YAML syntax

**Issue:** High memory usage  
**Fix:** Reduce `ChannelCapacity` in config, enable pagination

**Issue:** UI freezes  
**Fix:** Ensure long operations are async, use `Task.Run` for CPU-bound work

## ?? Further Reading

- [EF Core Docs](https://docs.microsoft.com/ef/core/)
- [Material Design XAML](http://materialdesigninxaml.net/)
- [LiveCharts2](https://livecharts.dev/)
- [YamlDotNet](https://github.com/aaubry/YamlDotNet)
- [Serilog](https://serilog.net/)
- [xUnit](https://xunit.net/)

---

**Quick help:** See `README.md` for full documentation or `DEMO.md` for walkthrough.
