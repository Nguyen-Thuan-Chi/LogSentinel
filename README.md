# LogSentinel

**Security Event Monitoring and Alert System**

LogSentinel is a comprehensive security information and event management (SIEM) solution built with .NET 9 and WPF. It provides real-time monitoring, rule-based alerting, and advanced analytics for Windows security events and custom log files.

## ?? Features

- **Real-time Event Monitoring** - Stream and process security events as they occur
- **Rule-Based Detection** - YAML-defined detection rules with flexible criteria
- **Advanced Alerting** - Multi-severity alerts with customizable actions
- **Full-Text Search** - SQLite FTS5 powered search across event details
- **Material Design UI** - Modern, responsive WPF interface
- **Data Visualization** - Charts and graphs for event trends
- **Export Capabilities** - Export alerts to JSON/CSV formats
- **Webhook Integration** - Send alerts to external systems
- **Dual Database Support** - SQLite for development, SQL Server for production

## ?? Prerequisites

- **.NET 9 SDK** or later ([Download](https://dotnet.microsoft.com/download/dotnet/9.0))
- **Windows 10/11** or Windows Server 2019+
- **Visual Studio 2022** (17.8+) or Rider 2024.1+ (optional, for development)
- **SQL Server 2019+** (optional, for production deployments)

## ??? Architecture

LogSentinel follows a clean 3-tier architecture:

```
„¡„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„¢
„   UI Layer (WPF - LogSentinel.UI)                        „ 
„   „¥„Ÿ Material Design XAML Toolkit                        „ 
„   „¥„Ÿ MVVM Pattern (CommunityToolkit.Mvvm)                „ 
„   „¤„Ÿ LiveCharts2 for visualizations                      „ 
„¥„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„§
„   Business Logic Layer (LogSentinel.BUS)                 „ 
„   „¥„Ÿ Event Normalizer (log parsing)                      „ 
„   „¥„Ÿ Rule Engine (YAML-based detection)                  „ 
„   „¥„Ÿ Alert Service (notification & export)               „ 
„   „¤„Ÿ Event Importer (streaming & batch)                  „ 
„¥„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„§
„   Data Access Layer (LogSentinel.DAL)                    „ 
„   „¥„Ÿ Entity Framework Core 9.0                           „ 
„   „¥„Ÿ Repository Pattern                                  „ 
„   „¥„Ÿ SQLite / SQL Server providers                       „ 
„   „¤„Ÿ FTS5 full-text search                               „ 
„¤„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„£
```

## ?? Quick Start

### 1. Clone and Build

```bash
git clone https://github.com/Nguyen-Thuan-Chi/LogSentinel.git
cd "Log Sentinel"
dotnet restore
dotnet build
```

### 2. Configure Database

**Option A: SQLite (Default)**
```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=./data/logsentinel.db"
  }
}
```

**Option B: SQL Server**
```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=LAPTOP-PN16MELH;Database=LogSentinel;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

### 3. Run Migrations

The application automatically applies migrations on startup. Alternatively:

```bash
cd LogSentinel.DAL
dotnet ef database update
```

### 4. Generate Sample Data

```powershell
# Generate 5000 synthetic events
.\scripts\Generate-SyntheticLogs.ps1 -EventCount 5000

# Continuous mode (append events every 5 seconds)
.\scripts\Generate-SyntheticLogs.ps1 -Continuous
```

### 5. Run the Application

```bash
cd "Log Sentinel"
dotnet run
```

Or press **F5** in Visual Studio.

## ?? Usage Guide

### Dashboard

The dashboard provides an at-a-glance view of system health:
- **Total Events Today** - Count of processed events
- **Recent Alerts** - Alerts from the last 5 minutes
- **Event Trends** - Hourly event distribution chart
- **Severity Breakdown** - Count by severity level

### Events View

Browse and search all ingested events:
- **Full-Text Search** - Search across all event fields
- **Filters** - Filter by host, user, event ID, level, date range
- **Pagination** - Handle large datasets efficiently
- **Context Menu** - Right-click for actions (create alert, pivot to graph, etc.)

### Rules View

Manage detection rules:
- **Enable/Disable** - Toggle rules on/off
- **Edit YAML** - Modify rule definitions
- **Test Rule** - Dry-run against selected events
- **View Stats** - Trigger count and last triggered time

### Creating Custom Rules

Rules are defined in YAML format:

```yaml
name: Failed Login Threshold
description: Detects multiple failed login attempts from the same user
severity: High
enabled: true
selection:
  event_id: 4625
condition:
  count: 5
  timeframe: 300  # seconds
  group_by: user
action:
  alert: true
  title: "Multiple Failed Logins for {user}"
  description: "{count} failed login attempts detected"
```

**Rule Components:**
- `selection` - Initial event filtering (event_id, level, provider, process, user, host)
- `condition` - Detection logic (count, timeframe, group_by, pattern, field)
- `action` - Response behavior (alert, title, description)

Place YAML files in: `LogSentinel.DAL\Data\Rules\`

## ?? Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~RuleEngineTests"
```

**Test Coverage:**
- ? EventNormalizer (log parsing validation)
- ? RuleEngine (3 sample rules: failed login, admin creation, suspicious PowerShell)
- ? AlertService (CRUD, export, webhook)

## ?? Integrations

### Webhook Configuration

Send alerts to external systems:

```json
// appsettings.json
{
  "LogSentinel": {
    "AlertWebhookUrl": "https://your-server.com/api/alerts"
  }
}
```

Alert payload:
```json
{
  "alert_id": 123,
  "rule_name": "Failed Login Threshold",
  "severity": "High",
  "timestamp": "2024-01-15T14:30:00Z",
  "title": "Multiple Failed Logins",
  "description": "5 failed login attempts detected"
}
```

### Windows Event Log Import

LogSentinel can import directly from Windows Event Logs:

```csharp
var eventNormalizer = serviceProvider.GetRequiredService<IEventNormalizer>();
var eventLog = new EventLog("Security");
foreach (EventLogEntry entry in eventLog.Entries) {
    var dto = eventNormalizer.NormalizeFromWindowsEvent(entry);
    // Process...
}
```

## ?? Configuration Reference

**appsettings.json**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=./data/logsentinel.db"
  },
  "LogSentinel": {
    "SampleLogsPath": "./sample-logs/incoming",
    "RulesPath": "./Data/Rules",
    "EnableFileWatcher": true,
    "AlertWebhookUrl": "",
    "BatchSize": 1000,
    "ChannelCapacity": 10000
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    },
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/logsentinel-.log" } }
    ]
  }
}
```

## ?? Performance

- **Event Processing** - Up to 10,000 events/second (in-memory channel)
- **Database** - Tested with 1M+ events in SQLite, 100M+ in SQL Server
- **FTS5 Search** - Sub-second full-text queries on 1M events
- **Rule Evaluation** - < 1ms per rule per event (compiled predicates)

**Batch Import Benchmark:**
```bash
# Import 10,000 events
Measure-Command { .\scripts\Import-Batch.ps1 -File "large-events.log" }
# Typical: 2-5 seconds
```

## ??? Development

### Project Structure

```
Log Sentinel/
„¥„Ÿ„Ÿ Log Sentinel/           # UI project (WPF)
„    „¥„Ÿ„Ÿ UI/                 # Views and controls
„    „¥„Ÿ„Ÿ ViewModels/         # MVVM view models
„    „¤„Ÿ„Ÿ appsettings.json
„¥„Ÿ„Ÿ LogSentinel.BUS/        # Business logic
„    „¥„Ÿ„Ÿ Interfaces/
„    „¥„Ÿ„Ÿ Models/
„    „¤„Ÿ„Ÿ Services/
„¥„Ÿ„Ÿ LogSentinel.DAL/        # Data access
„    „¥„Ÿ„Ÿ Data/               # Entities and DbContext
„    „¥„Ÿ„Ÿ Repositories/
„    „¤„Ÿ„Ÿ Migrations/
„¥„Ÿ„Ÿ LogSentinel.Tests/      # Unit tests
„¥„Ÿ„Ÿ sample-logs/            # Sample data
„¥„Ÿ„Ÿ scripts/                # Utility scripts
„¤„Ÿ„Ÿ README.md
```

### Adding a New Repository

1. Define interface in `LogSentinel.DAL\Repositories\`
2. Implement `EFRepository<TEntity>`
3. Register in `App.xaml.cs` DI container
4. Use via constructor injection

### Adding a New Rule

1. Create YAML file in `LogSentinel.DAL\Data\Rules\`
2. Define selection, condition, and action
3. Restart app or call `IRuleEngine.ReloadRulesAsync()`

## ?? Deployment

### SQLite Deployment (Standalone)

1. Publish the app:
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained
   ```
2. Copy output from `bin\Release\net9.0-windows\win-x64\publish\`
3. Ensure `data/` folder is writable

### SQL Server Deployment (Enterprise)

1. Create database:
   ```sql
   CREATE DATABASE LogSentinel;
   ```
2. Update connection string in `appsettings.Production.json`
3. Run migrations:
   ```bash
   dotnet ef database update --project LogSentinel.DAL
   ```
4. Deploy application

## ?? Sample Rules Included

| Rule Name | Event ID | Description |
|-----------|----------|-------------|
| Failed Login Threshold | 4625 | 5+ failed logins in 5 minutes |
| Admin User Created | 4732 | User added to Administrators group |
| Suspicious PowerShell | - | PowerShell with `-Enc`, `-NoP`, `-W Hidden` |
| Privilege Escalation | 4672 | Special privileges assigned |
| RDP Brute Force | 4625 | Multiple RDP login failures |
| Service Installation | 7045 | New service installed |
| Mimikatz Detection | - | Credential dumping tool signatures |
| Event Log Cleared | 1102 | Security log cleared |
| Scheduled Task Created | 4698 | Potentially malicious task |
| Account Lockout | 4740 | User account locked out |

## ?? Troubleshooting

**Issue: Database locked**
- Solution: Ensure only one instance of the app is running (SQLite limitation)

**Issue: Events not appearing**
- Check `sample-logs/incoming` folder exists and is writable
- Verify `EnableFileWatcher: true` in config
- Check logs in `logs/logsentinel-{date}.log`

**Issue: High memory usage**
- Reduce `ChannelCapacity` in config
- Decrease `BatchSize`
- Enable pagination in Events View

**Issue: Rules not triggering**
- Verify rule is enabled in Rules View
- Check rule YAML syntax with YAML validator
- Test rule against known matching event

## ?? License

This project is licensed under the MIT License - see [LICENSE](LICENSE) file for details.

## ?? Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## ?? Support

- **Issues**: [GitHub Issues](https://github.com/Nguyen-Thuan-Chi/LogSentinel/issues)
- **Email**: your-email@example.com
- **Documentation**: [Wiki](https://github.com/Nguyen-Thuan-Chi/LogSentinel/wiki)

## ?? Acknowledgments

- Material Design In XAML Toolkit
- LiveCharts2
- YamlDotNet
- Entity Framework Core
- Serilog
- xUnit

---

**Built with ?? using .NET 9 and WPF**
