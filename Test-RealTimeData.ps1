# Real-Time Data Test Script for LogSentinel
Write-Host "=== LogSentinel Real-Time Data Test ===" -ForegroundColor Green

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")

if ($isAdmin) {
    Write-Host "? Running as Administrator - Full access to Security Event Log" -ForegroundColor Green
} else {
    Write-Host "??  Not running as Administrator - Limited access to Security events" -ForegroundColor Yellow
    Write-Host "   Run as Administrator for full Windows Event Log access" -ForegroundColor Yellow
}

# Check Sysmon installation
$sysmonService = Get-Service -Name "Sysmon*" -ErrorAction SilentlyContinue
if ($sysmonService) {
    Write-Host "? Sysmon service found: $($sysmonService.Name) - Status: $($sysmonService.Status)" -ForegroundColor Green
} else {
    Write-Host "??  Sysmon not detected. Install from: https://docs.microsoft.com/sysinternals/downloads/sysmon" -ForegroundColor Yellow
}

# Check Event Log access
$eventLogs = @("System", "Application", "Security")
Write-Host "`n?? Event Log Status:" -ForegroundColor Cyan
foreach ($logName in $eventLogs) {
    try {
        $log = Get-WinEvent -LogName $logName -MaxEvents 1 -ErrorAction Stop
        Write-Host "  ? $logName - Accessible (Latest: $($log.TimeCreated))" -ForegroundColor Green
    } catch {
        Write-Host "  ? $logName - Access denied or empty" -ForegroundColor Red
    }
}

# Generate some test events
Write-Host "`n?? Generating test events..." -ForegroundColor Cyan

# Generate some Application events
try {
    Write-EventLog -LogName "Application" -Source "Application" -EventId 1001 -Message "LogSentinel Test Event - Application started" -EntryType Information
    Write-Host "  ? Generated Application event (ID: 1001)" -ForegroundColor Green
} catch {
    Write-Host "  ??  Could not generate Application event: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Try to generate some processes for Sysmon
Write-Host "  ?? Creating some processes for Sysmon detection..." -ForegroundColor Yellow
Start-Process "notepad.exe" -WindowStyle Hidden -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1
Get-Process "notepad" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

Start-Process "calc.exe" -WindowStyle Hidden -ErrorAction SilentlyContinue  
Start-Sleep -Seconds 1
Get-Process "Calculator*" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host "  ? Process events generated" -ForegroundColor Green

# Display configuration
Write-Host "`n??  Current Configuration:" -ForegroundColor Cyan
$configPath = "Log Sentinel\appsettings.json"
if (Test-Path $configPath) {
    $config = Get-Content $configPath | ConvertFrom-Json
    Write-Host "  EventLog: $($config.LogSentinel.Sources.EventLog)" -ForegroundColor White
    Write-Host "  Sysmon: $($config.LogSentinel.Sources.Sysmon)" -ForegroundColor White
    Write-Host "  SampleFiles: $($config.LogSentinel.Sources.SampleFiles)" -ForegroundColor White
    Write-Host "  Database: $($config.ConnectionStrings.DefaultConnection)" -ForegroundColor White
}

# Start the application
Write-Host "`n?? Starting LogSentinel..." -ForegroundColor Green
Write-Host "   Real-time events should appear in the Events tab" -ForegroundColor Yellow
Write-Host "   Dashboard will auto-refresh every 10 seconds" -ForegroundColor Yellow
Write-Host "   Events will auto-refresh every 5 seconds" -ForegroundColor Yellow
Write-Host "   Press Ctrl+C to stop" -ForegroundColor Yellow

Set-Location "Log Sentinel"
try {
    dotnet run
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    Set-Location ".."
}