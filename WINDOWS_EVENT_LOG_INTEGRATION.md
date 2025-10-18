# Windows Event Log & Sysmon Integration

## Overview

LogSentinel now supports real-time ingestion from:
- **Windows Event Logs**: Security, System, Application
- **Sysmon**: Microsoft-Windows-Sysmon/Operational (if installed)
- **Sample Log Files**: Legacy file-based ingestion (backward compatible)

All sources can run simultaneously and feed into the same normalized pipeline.

---

## Prerequisites

### 1. Administrator Privileges (Recommended)

To read the **Security** channel and access all event logs without restrictions, run LogSentinel as Administrator:

**Method 1: Right-click shortcut**
- Right-click `LogSentinel.exe` ¨ "Run as administrator"

**Method 2: Create shortcut with elevated permissions**
- Right-click `LogSentinel.exe` ¨ Create Shortcut
- Right-click shortcut ¨ Properties ¨ Advanced ¨ Check "Run as administrator"

### 2. Sysmon Installation (Optional)

Sysmon provides rich process, network, and file monitoring events.

**Install Sysmon:**
```powershell
# Download Sysmon from Microsoft Sysinternals
# https://docs.microsoft.com/sysinternals/downloads/sysmon

# Install with default config
sysmon64.exe -accepteula -i

# Or install with SwiftOnSecurity config (recommended)
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/SwiftOnSecurity/sysmon-config/master/sysmonconfig-export.xml" -OutFile "sysmonconfig.xml"
sysmon64.exe -accepteula -i sysmonconfig.xml
```

**Verify Sysmon is running:**
```powershell
Get-Service Sysmon64
# Status should be "Running"

# Check event log exists
Get-WinEvent -ListLog Microsoft-Windows-Sysmon/Operational
```

---

## Configuration

### Enable/Disable Sources

Edit `appsettings.json` or `appsettings.Development.json`:

```json
{
  "LogSentinel": {
    "Sources": {
      "SampleFiles": true,     // File-based sample logs
      "EventLog": true,         // Windows Event Logs (Security, System, Application)
      "Sysmon": true            // Sysmon operational log
    }
  }
}
```

**Production (appsettings.json)**: All sources disabled by default for safety  
**Development (appsettings.Development.json)**: All sources enabled for testing

### Channel Configuration

The application monitors these channels by default:
- `Security` (requires Admin)
- `System`
- `Application`
- `Microsoft-Windows-Sysmon/Operational` (if Sysmon installed)

Channels that don't exist or are inaccessible are automatically skipped with a warning logged.

---

## Testing the Integration

### 1. Generate Test Windows Events

**PowerShell - Create custom event:**
```powershell
# Create a custom event source (run as Admin, only once)
New-EventLog -LogName Application -Source "LogSentinelTest"

# Write test events
Write-EventLog -LogName Application -Source "LogSentinelTest" -EventId 1001 -EntryType Information -Message "Test INFO event from LogSentinel"
Write-EventLog -LogName Application -Source "LogSentinelTest" -EventId 1002 -EntryType Warning -Message "Test WARNING event from LogSentinel"
Write-EventLog -LogName Application -Source "LogSentinelTest" -EventId 1003 -EntryType Error -Message "Test ERROR event from LogSentinel"
```

**Verify in Event Viewer:**
```powershell
Get-WinEvent -LogName Application -MaxEvents 10 | Where-Object {$_.ProviderName -eq "LogSentinelTest"}
```

### 2. Generate Sysmon Test Events

**Trigger process creation events:**
```powershell
# Simple process creation (Sysmon Event ID 1)
notepad.exe
Start-Sleep -Seconds 2
Stop-Process -Name notepad -Force

# Network connection (Sysmon Event ID 3)
Test-NetConnection google.com -Port 443

# File creation (Sysmon Event ID 11)
echo "test" > $env:TEMP\sysmon-test.txt
Remove-Item $env:TEMP\sysmon-test.txt
```

**Verify Sysmon events:**
```powershell
Get-WinEvent -LogName Microsoft-Windows-Sysmon/Operational -MaxEvents 10
```

### 3. Monitor Real-Time Ingestion

1. Start LogSentinel (as Administrator recommended)
2. Navigate to **Events** view in UI
3. Run test commands above
4. Events should appear in the DataGrid within seconds
5. Use **Source Filter** dropdown to filter by:
   - All
   - Sample
   - WindowsEventLog
   - Sysmon

### 4. Check Dashboard Metrics

Navigate to **Dashboard** to see:
- **Total Events**: Count increases in real-time
- **Events/sec**: Live ingestion rate
- **Alerts**: Triggered by rule engine based on event patterns

---

## Permissions & Troubleshooting

### Issue: "Insufficient privileges to read Security channel"

**Symptom:** Warning message in logs or UI toast notification

**Solution:**
1. Run LogSentinel as Administrator (easiest)
2. OR grant specific ETW read permissions:

```powershell
# Grant current user read access to Security log (run as Admin)
wevtutil sl Security /ca:O:BAG:SYD:(A;;0xf0007;;;SY)(A;;0x7;;;BA)(A;;0x1;;;BO)(A;;0x1;;;SO)(A;;0x1;;;S-1-5-32-573)(A;;0x1;;;YourUsername)
```

Replace `YourUsername` with your Windows username.

### Issue: "Sysmon channel not found"

**Symptom:** Log message: "Sysmon channel 'Microsoft-Windows-Sysmon/Operational' not found"

**Solution:**
- Install Sysmon (see Prerequisites above)
- OR disable Sysmon in config: `"Sysmon": false`

### Issue: Events not appearing in UI

**Troubleshooting steps:**
1. Check **Logs** (`logs/logsentinel-*.log`) for errors
2. Verify sources are enabled in `appsettings.Development.json`
3. Confirm database path is writable: `./data/logsentinel.db`
4. Check Windows Event Log service is running:
   ```powershell
   Get-Service EventLog
   ```
5. Test event creation manually (see Testing section)

### Issue: High CPU/Memory usage

**Tuning:**
- Reduce channel capacity in config:
  ```json
  "ChannelCapacity": 5000  // Default: 10000
  ```
- Disable high-volume sources (e.g., Sysmon on busy systems)
- Add filters to Sysmon config to reduce event volume

---

## Sysmon Event Mapping

LogSentinel normalizes Sysmon events into standard fields:

| Sysmon Event ID | Action | Mapped Fields |
|-----------------|--------|---------------|
| 1 | Process Create | Process, ParentProcess, CommandLine, Hashes |
| 3 | Network Connection | DestinationIp, DestinationPort, Protocol |
| 5 | Process Terminated | Process, ProcessId |
| 7 | Image Loaded | ImageLoaded (DLL path) |
| 8 | CreateRemoteThread | Process, TargetProcess |
| 10 | Process Access | Process, TargetProcess |
| 11 | File Created | TargetFilename |
| 13 | Registry Value Set | TargetObject (registry path) |
| 22 | DNS Query | QueryName, QueryResults |
| 23 | File Delete | TargetFilename |

All raw Sysmon data is preserved in `DetailsJson` and `RawXml` fields for advanced analysis.

---

## Rule Engine Integration

Events from all sources (Sample, EventLog, Sysmon) are evaluated by the same rule engine.

**Example Rule (Sysmon process creation):**
```yaml
name: Suspicious PowerShell Execution
severity: High
conditions:
  - field: Source
    operator: equals
    value: Sysmon
  - field: EventId
    operator: equals
    value: 1
  - field: Process
    operator: contains
    value: powershell.exe
  - field: CommandLine
    operator: contains
    value: -EncodedCommand
actions:
  - type: Alert
    message: Detected PowerShell with encoded command
```

---

## Performance Considerations

- **Event Volume**: Sysmon can generate 1000+ events/second on busy systems
- **Channel Backpressure**: Bounded channel prevents memory overflow (drops events if full)
- **Deduplication**: Last 10,000 event IDs cached to prevent duplicates
- **Retry Policy**: Exponential backoff (5 retries) for transient errors

**Recommended Settings:**
- **Development/Testing**: All sources enabled, capacity 10000
- **Production**: Disable Sysmon or use filtered config, capacity 5000-10000

---

## Architecture

```
„¡„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„¢
„  Windows Event   „ 
„  Logs (ETW)      „ ?„Ÿ„Ÿ„Ÿ EventLogWatcher (Security, System, Application)
„¤„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„¦„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„£
         „ 
         „  EventRecord
         ¥
„¡„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„¢
„  WindowsEvent    „ 
„  Source          „ 
„¤„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„¦„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„£
         „ 
         „  EventDto (normalized)
         ¥
„¡„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„¢         „¡„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„¢
„  Bounded Channel „ ?„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„§ Sysmon       „ 
„  (10k capacity)  „          „  EventWatcher „ 
„¤„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„¦„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„£         „¤„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„£
         „ 
         „  EventDto
         ¥
„¡„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„¢
„  Event Importer  „ 
„  (background)    „ 
„¤„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„¦„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„£
         „ 
         „  EventEntity
         ¥
„¡„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„¢         „¡„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„¢
„  SQLite Database „ „Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ?„  Rule Engine  „ 
„¤„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„£         „¤„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„¦„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„£
                                   „ 
                                   ¥
                            „¡„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„¢
                            „  Alert Service„ 
                            „¤„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„£
```

---

## Commit History

Feature implementation divided into logical commits:

1. `feat: add IWindowsEventSource interface and models`
2. `feat: implement WindowsEventSource with EventLogWatcher`
3. `feat: add Sysmon-specific event normalizer`
4. `feat: extend EventImporter to support multiple sources`
5. `feat: add Source field to EventEntity and migration`
6. `feat: add configuration toggles for event sources`
7. `feat: update SettingsViewModel with source controls`
8. `feat: add source filter to EventsViewModel`
9. `docs: add Windows Event Log integration README`

---

## Next Steps

### Enhancements
- [ ] UI Settings panel for source toggles (avoid editing JSON)
- [ ] Real-time dashboard stats (events/sec by source)
- [ ] Export to SIEM (JSON, CEF, Syslog)
- [ ] Advanced Sysmon rule templates
- [ ] Event correlation engine (multi-event patterns)

### Testing
- [ ] Unit tests for WindowsEventSource
- [ ] Integration tests with mock EventLogWatcher
- [ ] Performance benchmarks (events/sec throughput)

---

## Resources

- [Sysmon Documentation](https://docs.microsoft.com/sysinternals/downloads/sysmon)
- [SwiftOnSecurity Sysmon Config](https://github.com/SwiftOnSecurity/sysmon-config)
- [Windows Event Log API](https://docs.microsoft.com/windows/win32/wes/windows-event-log)
- [ETW (Event Tracing for Windows)](https://docs.microsoft.com/windows-hardware/drivers/devtest/event-tracing-for-windows--etw-)

---

**Version:** 1.0  
**Last Updated:** 2025-01-18
