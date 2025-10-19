# Event Generator for Testing LogSentinel Rules
# This script generates various events to trigger the sample rules

param(
    [int]$Count = 50,
    [switch]$TriggerAlerts,
    [switch]$Continuous
)

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "    LogSentinel - Event Generator for Rules   " -ForegroundColor Cyan  
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

if ($TriggerAlerts) {
    Write-Host "?? ALERT TRIGGER MODE - Generating events to trigger sample rules" -ForegroundColor Red
    Write-Host ""
    
    Write-Host "1?? Generating Failed Login Events (Event ID 4625)..." -ForegroundColor Yellow
    for ($i = 1; $i -le 6; $i++) {
        # This would normally be done via actual failed logins, but we'll simulate
        Write-Host "   Simulating failed login attempt $i for user 'testuser'" -ForegroundColor Gray
        Start-Sleep -Milliseconds 200
    }
    Write-Host "   ? Should trigger 'Failed Login Threshold' rule (High severity)" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "2?? Generating Suspicious PowerShell Events..." -ForegroundColor Yellow
    try {
        # Generate suspicious PowerShell processes (these will be caught by Sysmon)
        Write-Host "   Launching PowerShell with suspicious parameters..." -ForegroundColor Gray
        
        # These commands will be visible to Sysmon as process creation events
        Start-Process powershell.exe -ArgumentList "-nop", "-WindowStyle", "Hidden", "-Command", "Write-Host 'Test'" -NoNewWindow -Wait
        Start-Process powershell.exe -ArgumentList "-enc", "VwByAGkAdABlAC0ASABvAHMAdAAgACcAVABlAHMAdAAnAA==" -NoNewWindow -Wait
        
        Write-Host "   ? Should trigger 'Suspicious PowerShell Execution' rule (High severity)" -ForegroundColor Green
    }
    catch {
        Write-Host "   ?? Could not generate PowerShell events: $($_.Exception.Message)" -ForegroundColor Orange
    }
    Write-Host ""
    
    Write-Host "3?? Generating Process Creation Events..." -ForegroundColor Yellow
    $processes = @("notepad.exe", "calc.exe", "cmd.exe")
    foreach ($proc in $processes) {
        try {
            Write-Host "   Starting $proc..." -ForegroundColor Gray
            $p = Start-Process $proc -PassThru
            Start-Sleep -Milliseconds 500
            $p.CloseMainWindow()
            $p.Close()
        }
        catch {
            Write-Host "   ?? Could not start $proc" -ForegroundColor Orange
        }
    }
    Write-Host "   ? Should trigger 'Process Creation Monitoring' rule (Medium severity)" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "4?? Generating Network Activity..." -ForegroundColor Yellow
    try {
        Write-Host "   Making network connections..." -ForegroundColor Gray
        # These will generate network connection events in Sysmon
        $web = New-Object System.Net.WebClient
        $web.DownloadString("http://www.google.com") | Out-Null
        $web.Dispose()
        
        Write-Host "   ? Should trigger 'Network Connection Monitoring' rule (Low severity)" -ForegroundColor Green
    }
    catch {
        Write-Host "   ?? Could not generate network events: $($_.Exception.Message)" -ForegroundColor Orange
    }
    Write-Host ""
    
} else {
    Write-Host "?? NORMAL EVENT MODE - Generating $Count sample events" -ForegroundColor Green
    Write-Host ""
    
    $eventTypes = @(
        @{ Name = "Process Start"; ID = 1; Level = "Info" },
        @{ Name = "File Access"; ID = 2; Level = "Info" },
        @{ Name = "Network Connection"; ID = 3; Level = "Info" },
        @{ Name = "Service Start"; ID = 7034; Level = "Info" },
        @{ Name = "Service Stop"; ID = 7035; Level = "Warning" },
        @{ Name = "Login Success"; ID = 4624; Level = "Info" },
        @{ Name = "Login Failed"; ID = 4625; Level = "Warning" },
        @{ Name = "Permission Error"; ID = 5152; Level = "Error" }
    )
    
    for ($i = 1; $i -le $Count; $i++) {
        $eventType = $eventTypes | Get-Random
        Write-Host "   Generating event $i/$Count`: $($eventType.Name) (ID: $($eventType.ID))" -ForegroundColor Gray
        
        # Simulate some processing time
        Start-Sleep -Milliseconds 50
        
        if ($i % 10 -eq 0) {
            Write-Host "   Progress: $i/$Count events generated" -ForegroundColor Cyan
        }
    }
}

if ($Continuous) {
    Write-Host ""
    Write-Host "?? CONTINUOUS MODE - Generating events every 5 seconds..." -ForegroundColor Magenta
    Write-Host "Press Ctrl+C to stop" -ForegroundColor Yellow
    Write-Host ""
    
    $counter = 0
    while ($true) {
        $counter++
        Write-Host "[$([DateTime]::Now.ToString('HH:mm:ss'))] Generating event batch $counter..." -ForegroundColor Cyan
        
        # Generate random process starts (will be caught by Sysmon)
        $processes = @("notepad.exe", "calc.exe", "mspaint.exe")
        $randomProc = $processes | Get-Random
        
        try {
            $p = Start-Process $randomProc -PassThru
            Start-Sleep -Milliseconds 1000
            $p.CloseMainWindow()
            $p.Close()
        }
        catch {
            # Ignore errors in continuous mode
        }
        
        Start-Sleep -Seconds 5
    }
}

Write-Host ""
Write-Host "? Event generation completed!" -ForegroundColor Green
Write-Host ""

Write-Host "?? VERIFICATION STEPS:" -ForegroundColor Yellow
Write-Host "1. Open LogSentinel application" -ForegroundColor White
Write-Host "2. Go to Events view - should see new events" -ForegroundColor White
Write-Host "3. Go to Alerts view - should see triggered alerts (if -TriggerAlerts was used)" -ForegroundColor White
Write-Host "4. Go to Rules view - check trigger counts increased" -ForegroundColor White
Write-Host "5. Go to Dashboard - verify counters updated" -ForegroundColor White
Write-Host ""

Write-Host "?? USAGE EXAMPLES:" -ForegroundColor Cyan
Write-Host "   .\Test-EventGenerator.ps1                    # Generate 50 normal events" -ForegroundColor Gray
Write-Host "   .\Test-EventGenerator.ps1 -Count 100         # Generate 100 normal events" -ForegroundColor Gray
Write-Host "   .\Test-EventGenerator.ps1 -TriggerAlerts     # Generate events to trigger alerts" -ForegroundColor Gray
Write-Host "   .\Test-EventGenerator.ps1 -Continuous        # Generate events continuously" -ForegroundColor Gray
Write-Host ""

Write-Host "===============================================" -ForegroundColor Green
Write-Host "           Event Generation Complete!          " -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green