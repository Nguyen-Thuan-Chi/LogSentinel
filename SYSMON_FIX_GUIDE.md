# LogSentinel - Sysmon Integration Fix Guide

## V?n ?? ???c ph?t hi?n

B?n ?? c?i ??t Sysmon (`sysmon.exe`) v? service ?ang ch?y, nh?ng LogSentinel kh?ng th? k?t n?i ???c v?:

1. **Event log channel kh?ng t?n t?i**: `Microsoft-Windows-Sysmon/Operational`
2. **Sysmon thi?u configuration**: Sysmon c?n file c?u h?nh ?? t?o event log

## Nguy?n nh?n

Khi Sysmon ???c c?i ??t m? kh?ng c? configuration file, n? s?:
- Ch?y service nh?ng kh?ng t?o event log channel
- Kh?ng generate events v?o Windows Event Log
- LogSentinel kh?ng th? k?t n?i v? channel kh?ng t?n t?i

## Gi?i ph?p

### B??c 1: Ch?y script fix (Khuy?n ngh?)

```powershell
# Ch?y PowerShell as Administrator
.\Fix-SysmonConfig.ps1
```

Script n?y s?:
- Ki?m tra Sysmon hi?n t?i
- Uninstall v? reinstall v?i configuration ??ng
- Verify k?t qu?

### B??c 2: Fix th? c?ng (n?u script kh?ng ho?t ??ng)

```powershell
# 1. Uninstall Sysmon hi?n t?i
sysmon -u force

# 2. Install l?i v?i config file
sysmon -accepteula -i sysmon-config.xml

# 3. Verify
Get-Service Sysmon
Get-WinEvent -ListLog Microsoft-Windows-Sysmon/Operational
```

### B??c 3: Test k?t qu?

```powershell
# Test connection
.\Test-SysmonConnection.ps1

# Generate test events
.\Test-EventGeneration-Fixed.ps1
```

## Files ???c t?o

1. **sysmon-config.xml** - Configuration file cho Sysmon
2. **Fix-SysmonConfig.ps1** - Script t? ??ng fix
3. **Test-SysmonConnection.ps1** - Test k?t n?i Sysmon
4. **Test-EventGeneration-Fixed.ps1** - Generate test events (?? fix)

## Verification Steps

### 1. Ki?m tra Service
```powershell
Get-Service Sysmon
# Status should be "Running"
```

### 2. Ki?m tra Event Log Channel
```powershell
Get-WinEvent -ListLog Microsoft-Windows-Sysmon/Operational
# Should return log information, not error
```

### 3. Ki?m tra Events
```powershell
Get-WinEvent -LogName Microsoft-Windows-Sysmon/Operational -MaxEvents 5
# Should return recent Sysmon events
```

### 4. Test trong LogSentinel
1. Start LogSentinel as Administrator
2. Navigate to Events view
3. Filter by Source = "Sysmon"
4. Generate activity (open apps, browse web)
5. Should see real-time Sysmon events

## Sysmon Event Types ???c monitor

| Event ID | Type | Description |
|----------|------|-------------|
| 1 | Process Creation | New processes started |
| 3 | Network Connection | TCP/UDP connections |
| 5 | Process Termination | Processes ended |
| 11 | File Creation | Files created |
| 13 | Registry Value Set | Registry modifications |
| 22 | DNS Query | DNS lookups |

## Troubleshooting

### Issue: "Access denied" khi ch?y scripts
**Solution**: Ch?y PowerShell as Administrator

### Issue: Event log v?n kh?ng t?n t?i sau fix
**Solution**: 
1. Restart m?y
2. Reinstall Sysmon ho?n to?n:
   ```powershell
   sysmon -u force
   # Restart
   sysmon -accepteula -i sysmon-config.xml
   ```

### Issue: LogSentinel kh?ng hi?n th? Sysmon events
**Solution**:
1. Verify appsettings.json c? `"Sysmon": true`
2. Restart LogSentinel as Administrator
3. Check logs for error messages

### Issue: Too many events
**Solution**: Modify `sysmon-config.xml` ?? filter events:
```xml
<!-- Exclude noisy processes -->
<ProcessCreate onmatch="exclude">
  <Image condition="is">C:\Windows\System32\svchost.exe</Image>
</ProcessCreate>
```

## Configuration Details

Sysmon configuration ???c tune ??:
- Monitor security-relevant activities
- Filter out noisy system processes
- Include network connections, file changes, registry changes
- Generate reasonable event volume

Configuration c? th? ???c customize theo nhu c?u b?o m?t c? th?.

## Next Steps

Sau khi Sysmon ho?t ??ng:
1. Configure rules trong LogSentinel ?? detect suspicious activities
2. Set up alerts cho critical Sysmon events
3. Monitor dashboard cho Sysmon event statistics
4. Consider tuning Sysmon config ?? optimize performance

## Support

N?u v?n g?p v?n ??:
1. Check Windows Event Viewer Å® Applications and Services Logs Å® Microsoft-Windows-Sysmon/Operational
2. Run `sysmon -c` ?? xem current configuration
3. Check LogSentinel logs trong `logs/` directory
4. Verify permissions (ch?y LogSentinel as Administrator)