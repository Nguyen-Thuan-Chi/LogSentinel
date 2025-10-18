# LogSentinel Event Generation Test Script
# Purpose: Generate test events for Windows Event Log and Sysmon ingestion
# Run as Administrator for best results

Write-Host "?????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?          LogSentinel Event Generation Test Script            ?" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "[WARNING] Not running as Administrator. Some tests may fail." -ForegroundColor Yellow
    Write-Host "          Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    Write-Host ""
}

# Function to create event source
function Initialize-TestEventSource {
    param([string]$SourceName = "LogSentinelTest")
    
    Write-Host "[1/4] Checking event source..." -ForegroundColor Cyan
    
    try {
        if (-not [System.Diagnostics.EventLog]::SourceExists($SourceName)) {
            Write-Host "      Creating event source '$SourceName'..." -ForegroundColor Yellow
            New-EventLog -LogName Application -Source $SourceName -ErrorAction Stop
            Write-Host "      ? Event source created successfully" -ForegroundColor Green
        } else {
            Write-Host "      ? Event source '$SourceName' already exists" -ForegroundColor Green
        }
        return $true
    }
    catch {
        Write-Host "      ? Failed to create event source: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "      Run this script as Administrator" -ForegroundColor Red
        return $false
    }
}

# Function to generate Windows Event Log test events
function New-TestWindowsEvents {
    param([string]$SourceName = "LogSentinelTest")
    
    Write-Host "[2/4] Generating Windows Event Log test events..." -ForegroundColor Cyan
    
    $events = @(
        @{ Id = 1001; Type = "Information"; Message = "Test INFO event - Application started successfully" },
        @{ Id = 1002; Type = "Warning"; Message = "Test WARNING event - High memory usage detected (85%)" },
        @{ Id = 1003; Type = "Error"; Message = "Test ERROR event - Failed to connect to database server" },
        @{ Id = 1004; Type = "Information"; Message = "Test INFO event - User authentication successful" },
        @{ Id = 2001; Type = "Warning"; Message = "Test WARNING event - Suspicious login attempt from unknown IP" }
    )
    
    $count = 0
    foreach ($event in $events) {
        try {
            Write-EventLog -LogName Application -Source $SourceName `
                -EventId $event.Id -EntryType $event.Type -Message $event.Message `
                -ErrorAction Stop
            $count++
            Write-Host "      ? Created $($event.Type) event (ID: $($event.Id))" -ForegroundColor Green
            Start-Sleep -Milliseconds 200
        }
        catch {
            Write-Host "      ? Failed to create event: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    Write-Host "      Generated $count Windows Event Log entries" -ForegroundColor Cyan
    return $count
}

# Function to check Sysmon installation
function Test-SysmonInstalled {
    Write-Host "[3/4] Checking Sysmon installation..." -ForegroundColor Cyan
    
    try {
        $sysmonService = Get-Service -Name Sysmon64 -ErrorAction SilentlyContinue
        if ($null -eq $sysmonService) {
            $sysmonService = Get-Service -Name Sysmon -ErrorAction SilentlyContinue
        }
        
        if ($null -ne $sysmonService -and $sysmonService.Status -eq "Running") {
            Write-Host "      ? Sysmon is installed and running ($($sysmonService.Name))" -ForegroundColor Green
            
            # Check if event log exists
            $logExists = Get-WinEvent -ListLog Microsoft-Windows-Sysmon/Operational -ErrorAction SilentlyContinue
            if ($null -ne $logExists) {
                Write-Host "      ? Sysmon event log is available" -ForegroundColor Green
                return $true
            }
        }
        
        Write-Host "      ? Sysmon is not installed or not running" -ForegroundColor Yellow
        Write-Host "      Install from: https://docs.microsoft.com/sysinternals/downloads/sysmon" -ForegroundColor Yellow
        return $false
    }
    catch {
        Write-Host "      ? Error checking Sysmon: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to generate Sysmon test events
function New-TestSysmonEvents {
    Write-Host "[4/4] Generating Sysmon test events..." -ForegroundColor Cyan
    
    $count = 0
    
    # Event ID 1: Process Creation
    Write-Host "      Triggering process creation events..." -ForegroundColor White
    try {
        # Start and stop notepad (triggers Sysmon Event ID 1 and 5)
        $proc1 = Start-Process notepad.exe -PassThru -WindowStyle Hidden
        Start-Sleep -Milliseconds 500
        Stop-Process -Id $proc1.Id -Force -ErrorAction SilentlyContinue
        $count++
        Write-Host "      ? Process creation event (notepad.exe)" -ForegroundColor Green
    }
    catch {
        Write-Host "      ? Failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Start-Sleep -Milliseconds 300
    
    try {
        # Trigger calc.exe
        $proc2 = Start-Process calc.exe -PassThru -WindowStyle Hidden
        Start-Sleep -Milliseconds 500
        Stop-Process -Id $proc2.Id -Force -ErrorAction SilentlyContinue
        $count++
        Write-Host "      ? Process creation event (calc.exe)" -ForegroundColor Green
    }
    catch {
        Write-Host "      ? Failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Start-Sleep -Milliseconds 300
    
    # Event ID 3: Network Connection
    Write-Host "      Triggering network connection events..." -ForegroundColor White
    try {
        Test-NetConnection google.com -Port 443 -WarningAction SilentlyContinue | Out-Null
        $count++
        Write-Host "      ? Network connection event (HTTPS to google.com)" -ForegroundColor Green
    }
    catch {
        Write-Host "      ? Failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Start-Sleep -Milliseconds 300
    
    # Event ID 11: File Creation
    Write-Host "      Triggering file creation events..." -ForegroundColor White
    try {
        $testFile = Join-Path $env:TEMP "logsentinel-test-$(Get-Date -Format 'yyyyMMddHHmmss').txt"
        "Test content for Sysmon Event ID 11" | Out-File -FilePath $testFile -Force
        Start-Sleep -Milliseconds 200
        Remove-Item -Path $testFile -Force -ErrorAction SilentlyContinue
        $count++
        Write-Host "      ? File creation event ($testFile)" -ForegroundColor Green
    }
    catch {
        Write-Host "      ? Failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Start-Sleep -Milliseconds 300
    
    # Event ID 13: Registry modification (requires admin)
    if ($isAdmin) {
        Write-Host "      Triggering registry events..." -ForegroundColor White
        try {
            $testKey = "HKCU:\Software\LogSentinelTest"
            if (-not (Test-Path $testKey)) {
                New-Item -Path $testKey -Force | Out-Null
            }
            Set-ItemProperty -Path $testKey -Name "TestValue" -Value "LogSentinel" -Force
            Start-Sleep -Milliseconds 200
            Remove-Item -Path $testKey -Recurse -Force -ErrorAction SilentlyContinue
            $count++
            Write-Host "      ? Registry modification event" -ForegroundColor Green
        }
        catch {
            Write-Host "      ? Failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    Write-Host "      Generated ~$count Sysmon events" -ForegroundColor Cyan
    return $count
}

# Main execution
Write-Host ""
Write-Host "Starting event generation tests..." -ForegroundColor White
Write-Host ""

# Initialize event source
if (Initialize-TestEventSource) {
    # Generate Windows events
    $winEventCount = New-TestWindowsEvents
    
    Write-Host ""
    
    # Check and generate Sysmon events
    if (Test-SysmonInstalled) {
        $sysmonEventCount = New-TestSysmonEvents
    } else {
        $sysmonEventCount = 0
    }
    
    Write-Host ""
    Write-Host "???????????????????????????????????????????????????????????????" -ForegroundColor Cyan
    Write-Host "Test Summary:" -ForegroundColor Cyan
    Write-Host "  Windows Event Log entries: $winEventCount" -ForegroundColor White
    Write-Host "  Sysmon events triggered:   ~$sysmonEventCount" -ForegroundColor White
    Write-Host "???????????????????????????????????????????????????????????????" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "Next Steps:" -ForegroundColor Yellow
    Write-Host "  1. Start LogSentinel (as Administrator recommended)" -ForegroundColor White
    Write-Host "  2. Enable event sources in appsettings.Development.json:" -ForegroundColor White
    Write-Host "       'EventLog': true" -ForegroundColor Gray
    Write-Host "       'Sysmon': true" -ForegroundColor Gray
    Write-Host "  3. Navigate to Events view to see ingested events" -ForegroundColor White
    Write-Host "  4. Filter by Source: 'WindowsEventLog' or 'Sysmon'" -ForegroundColor White
    Write-Host ""
    
    # Verification commands
    Write-Host "Verify events in Event Viewer (PowerShell):" -ForegroundColor Yellow
    Write-Host "  Get-WinEvent -LogName Application -MaxEvents 10 | Where-Object {`$_.ProviderName -eq 'LogSentinelTest'}" -ForegroundColor Gray
    
    if ($sysmonEventCount -gt 0) {
        Write-Host "  Get-WinEvent -LogName Microsoft-Windows-Sysmon/Operational -MaxEvents 10" -ForegroundColor Gray
    }
    Write-Host ""
    
} else {
    Write-Host ""
    Write-Host "? Test aborted due to initialization failure" -ForegroundColor Red
    Write-Host "  Please run this script as Administrator" -ForegroundColor Yellow
    Write-Host ""
}

Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
