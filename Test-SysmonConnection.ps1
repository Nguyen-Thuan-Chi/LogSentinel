# Test Sysmon Connection for LogSentinel
# Run as Administrator

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "    LogSentinel - Sysmon Connection Test       " -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "[WARNING] Not running as Administrator. Some tests may fail." -ForegroundColor Yellow
    Write-Host ""
}

Write-Host "[1] Checking Sysmon Service..." -ForegroundColor Green

# Check Sysmon service
$sysmonService = Get-Service -Name Sysmon -ErrorAction SilentlyContinue
if ($null -ne $sysmonService) {
    Write-Host "    ? Sysmon service: $($sysmonService.Status)" -ForegroundColor White
    Write-Host "    Service Name: $($sysmonService.Name)" -ForegroundColor Gray
    Write-Host "    Display Name: $($sysmonService.DisplayName)" -ForegroundColor Gray
} else {
    Write-Host "    ? Sysmon service not found!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "[2] Checking Sysmon Event Log..." -ForegroundColor Green

# Test event log availability
try {
    $sysmonLog = Get-WinEvent -ListLog Microsoft-Windows-Sysmon/Operational -ErrorAction Stop
    Write-Host "    ? Sysmon event log found!" -ForegroundColor White
    Write-Host "    Log enabled: $($sysmonLog.IsEnabled)" -ForegroundColor Gray
    Write-Host "    Max size: $($sysmonLog.MaximumSizeInBytes / 1MB) MB" -ForegroundColor Gray
    Write-Host "    Record count: $($sysmonLog.RecordCount)" -ForegroundColor Gray
    Write-Host "    File path: $($sysmonLog.LogFilePath)" -ForegroundColor Gray
}
catch {
    Write-Host "    ? Sysmon event log NOT available!" -ForegroundColor Red
    Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "    This indicates Sysmon is not properly configured." -ForegroundColor Yellow
    Write-Host "    Run Fix-SysmonConfig.ps1 as Administrator to fix this." -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "[3] Testing Event Log Reader..." -ForegroundColor Green

# Test if we can read events
try {
    $query = New-Object System.Diagnostics.Eventing.Reader.EventLogQuery("Microsoft-Windows-Sysmon/Operational", [System.Diagnostics.Eventing.Reader.PathType]::LogName)
    $reader = New-Object System.Diagnostics.Eventing.Reader.EventLogReader($query)
    
    Write-Host "    ? Event log reader created successfully" -ForegroundColor White
    
    # Try to read last 5 events
    $eventCount = 0
    while ($eventCount -lt 5) {
        $event = $reader.ReadEvent()
        if ($null -eq $event) {
            break
        }
        $eventCount++
        Write-Host "    Found event ID $($event.Id) at $($event.TimeCreated)" -ForegroundColor Gray
        $event.Dispose()
    }
    
    if ($eventCount -eq 0) {
        Write-Host "    ??  No events found in Sysmon log" -ForegroundColor Yellow
        Write-Host "    This is normal if no activity has occurred recently" -ForegroundColor Gray
    } else {
        Write-Host "    ? Successfully read $eventCount Sysmon events" -ForegroundColor White
    }
    
    $reader.Dispose()
}
catch {
    Write-Host "    ? Failed to read Sysmon events!" -ForegroundColor Red
    Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "    This may indicate permission issues." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "[4] Testing Event Watcher..." -ForegroundColor Green

# Test event watcher (like LogSentinel uses)
try {
    $query = New-Object System.Diagnostics.Eventing.Reader.EventLogQuery("Microsoft-Windows-Sysmon/Operational", [System.Diagnostics.Eventing.Reader.PathType]::LogName)
    $watcher = New-Object System.Diagnostics.Eventing.Reader.EventLogWatcher($query)
    
    Write-Host "    ? Event watcher created successfully" -ForegroundColor White
    
    # Set up event handler
    $eventReceived = $false
    $eventHandler = {
        param($sender, $args)
        if ($args.EventRecord -ne $null) {
            Write-Host "    ? Real-time event received: ID $($args.EventRecord.Id)" -ForegroundColor Green
            $script:eventReceived = $true
        }
    }
    
    $watcher.add_EventRecordWritten($eventHandler)
    $watcher.Enabled = $true
    
    Write-Host "    Listening for new Sysmon events for 5 seconds..." -ForegroundColor White
    Write-Host "    (Generate some activity to test: open Notepad, browse websites, etc.)" -ForegroundColor Gray
    
    Start-Sleep -Seconds 5
    
    $watcher.Enabled = $false
    $watcher.Dispose()
    
    if ($eventReceived) {
        Write-Host "    ? Real-time event monitoring working!" -ForegroundColor Green
    } else {
        Write-Host "    ??  No real-time events received during test" -ForegroundColor Yellow
        Write-Host "    This may be normal if no monitored activity occurred" -ForegroundColor Gray
    }
}
catch {
    Write-Host "    ? Failed to create event watcher!" -ForegroundColor Red
    Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "[5] Checking Sysmon Configuration..." -ForegroundColor Green

# Check current Sysmon config
try {
    $configOutput = sysmon -c 2>&1
    if ($configOutput -match "Current schema version") {
        Write-Host "    ? Sysmon is configured" -ForegroundColor White
        Write-Host "    $configOutput" -ForegroundColor Gray
    } else {
        Write-Host "    ??  Sysmon configuration unclear" -ForegroundColor Yellow
        Write-Host "    Output: $configOutput" -ForegroundColor Gray
    }
}
catch {
    Write-Host "    ? Cannot check Sysmon configuration" -ForegroundColor Red
    Write-Host "    Run as Administrator to check configuration" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan

if ($eventReceived) {
    Write-Host "               ? ALL TESTS PASSED!           " -ForegroundColor Green
    Write-Host "Sysmon is properly configured and working." -ForegroundColor White
    Write-Host "LogSentinel should be able to receive Sysmon events." -ForegroundColor White
} else {
    Write-Host "             ??  PARTIAL SUCCESS             " -ForegroundColor Yellow
    Write-Host "Sysmon is installed but may need configuration." -ForegroundColor White
    Write-Host "Run Fix-SysmonConfig.ps1 if issues persist." -ForegroundColor White
}

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. If tests passed, start LogSentinel as Administrator" -ForegroundColor White
Write-Host "  2. Generate activity with Test-EventGeneration-Fixed.ps1" -ForegroundColor White
Write-Host "  3. Check Events view with Source filter = 'Sysmon'" -ForegroundColor White
Write-Host ""

Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")