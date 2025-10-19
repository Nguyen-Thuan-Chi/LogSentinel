# LogSentinel - Sysmon Integration Fix Summary

## V?n ?? ?? x?c ??nh ?

B?n ?? c?i ??t Sysmon nh?ng LogSentinel kh?ng th? k?t n?i v?:
- **Sysmon service ?ang ch?y** ?
- **Event log channel kh?ng t?n t?i** ? `Microsoft-Windows-Sysmon/Operational`
- **Sysmon thi?u configuration file** ?

## C?c thay ??i ?? th?c hi?n

### 1. Scripts ???c t?o/s?a

| File | M? t? | Status |
|------|-------|--------|
| `sysmon-config.xml` | Configuration file cho Sysmon | ? T?o m?i |
| `Fix-SysmonConfig.ps1` | Script t? ??ng fix Sysmon | ? T?o m?i |
| `Test-SysmonConnection.ps1` | Test k?t n?i Sysmon chi ti?t | ? T?o m?i |
| `Test-EventGeneration-Fixed.ps1` | Script test events ?? fix | ? T?o m?i |
| `Test-EventGeneration.ps1` | Script g?c ?? s?a detection logic | ? ?? s?a |
| `SYSMON_FIX_GUIDE.md` | H??ng d?n chi ti?t | ? T?o m?i |

### 2. Code fixes

| Component | Fix | Status |
|-----------|-----|--------|
| `Test-EventGeneration.ps1` | S?a Sysmon detection logic | ? |
| `WindowsEventSource.cs` | ?? c? s?n logic x? l? Sysmon | ? |
| `EventNormalizer.cs` | ?? c? method `NormalizeFromSysmonEvent` | ? |
| `appsettings.json` | Sysmon ?? enabled | ? |

## H??ng d?n th?c hi?n ngay

### B??c 1: Fix Sysmon (Quan tr?ng nh?t)
```powershell
# Ch?y PowerShell as Administrator
cd "C:\taro\workspace\visual studio\wpf\Log Sentinel"
.\Fix-SysmonConfig.ps1
```

### B??c 2: Test k?t qu?
```powershell
.\Test-SysmonConnection.ps1
```

### B??c 3: Generate test events
```powershell
.\Test-EventGeneration-Fixed.ps1
```

### B??c 4: Test LogSentinel
1. Start LogSentinel **as Administrator**
2. Navigate to Events view
3. Filter by Source = "Sysmon"
4. Generate activity (open apps, browse web)

## Expected Results

### Sau khi ch?y Fix-SysmonConfig.ps1:
```
? Sysmon event log is now available!
Log enabled: True
Service status: Running
```

### Trong LogSentinel Events view:
- **Source filter** c? option "Sysmon"
- **Real-time Sysmon events** xu?t hi?n
- **Event types**: Process Create, Network Connection, File Create, etc.

## Event Types s? th?y

| Event ID | Type | V? d? |
|----------|------|-------|
| 1 | Process Creation | notepad.exe started |
| 3 | Network Connection | Connection to google.com:443 |
| 5 | Process Termination | notepad.exe ended |
| 11 | File Creation | File created in temp folder |
| 13 | Registry Set | Registry value modified |
| 22 | DNS Query | DNS lookup for google.com |

## Verification Commands

### Check Sysmon Service:
```powershell
Get-Service Sysmon
```

### Check Event Log:
```powershell
Get-WinEvent -ListLog Microsoft-Windows-Sysmon/Operational
```

### View Recent Events:
```powershell
Get-WinEvent -LogName Microsoft-Windows-Sysmon/Operational -MaxEvents 5
```

## Root Cause Analysis

**Problem**: Sysmon ???c c?i ??t m? kh?ng c? configuration file
**Impact**: Service ch?y nh?ng kh?ng t?o event log channel
**Solution**: Reinstall Sysmon v?i proper configuration

## Technical Details

### Sysmon Channel Detection trong LogSentinel:
```csharp
private const string SysmonChannel = "Microsoft-Windows-Sysmon/Operational";

// WindowsEventSource.cs line ~45
if (enableSysmon && IsChannelAvailable(SysmonChannel))
{
    activeChannels.Add(SysmonChannel);
    tasks.Add(WatchChannelAsync(SysmonChannel, eventChannel, "Sysmon", cancellationToken));
}
```

### Event Normalization:
```csharp
// EventNormalizer.cs c? method ri?ng cho Sysmon
public EventDto NormalizeFromSysmonEvent(EventRecord eventRecord)
{
    // Parses Sysmon-specific XML data
    // Maps event IDs to actions
    // Extracts process, network, file info
}
```

## Next Steps

1. **Fix Sysmon** b?ng script ???c cung c?p
2. **Test connection** v?i Test-SysmonConnection.ps1
3. **Start LogSentinel** as Administrator
4. **Generate activity** v? verify events trong UI
5. **Configure rules** ?? detect suspicious Sysmon events

## Troubleshooting

### N?u script fix kh?ng ho?t ??ng:
1. Ch?y manual commands trong SYSMON_FIX_GUIDE.md
2. Restart m?y sau khi reinstall Sysmon
3. Check Windows Event Viewer manually

### N?u LogSentinel v?n kh?ng th?y events:
1. Verify app ch?y as Administrator
2. Check logs trong `logs/logsentinel-*.log`
3. Restart LogSentinel sau khi fix Sysmon

## Files created cho b?n

T?t c? files ?? ???c t?o trong project directory:
- Scripts ?? fix v? test
- Configuration file cho Sysmon
- Documentation chi ti?t

**B??c ti?p theo**: Ch?y `.\Fix-SysmonConfig.ps1` as Administrator!