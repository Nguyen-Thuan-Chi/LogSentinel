# Fix Sysmon Configuration for LogSentinel
# Run this script as Administrator to properly configure Sysmon

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "    LogSentinel - Sysmon Configuration Fix     " -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "[ERROR] This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "        Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Press any key to exit..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}

Write-Host "[Step 1] Checking current Sysmon installation..." -ForegroundColor Green

# Check if Sysmon service exists
$sysmonService = Get-Service -Name Sysmon -ErrorAction SilentlyContinue
if ($null -ne $sysmonService) {
    Write-Host "         Sysmon service found: $($sysmonService.Status)" -ForegroundColor White
    
    # Check current configuration
    try {
        $config = sysmon -c 2>&1
        Write-Host "         Current configuration:" -ForegroundColor White
        Write-Host "         $config" -ForegroundColor Gray
    }
    catch {
        Write-Host "         Could not retrieve current configuration" -ForegroundColor Yellow
    }
} else {
    Write-Host "         [ERROR] Sysmon service not found!" -ForegroundColor Red
    Write-Host "         Please install Sysmon first from: https://docs.microsoft.com/sysinternals/downloads/sysmon" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "[Step 2] Checking Sysmon event log availability..." -ForegroundColor Green

# Check if Sysmon event log exists
try {
    $sysmonLog = Get-WinEvent -ListLog Microsoft-Windows-Sysmon/Operational -ErrorAction Stop
    Write-Host "         ? Sysmon event log is available" -ForegroundColor Green
    Write-Host "         Log enabled: $($sysmonLog.IsEnabled)" -ForegroundColor White
    Write-Host "         Record count: $($sysmonLog.RecordCount)" -ForegroundColor White
}
catch {
    Write-Host "         ? Sysmon event log NOT available" -ForegroundColor Red
    Write-Host "         This means Sysmon is not properly configured" -ForegroundColor Yellow
    
    Write-Host ""
    Write-Host "[Step 3] Reconfiguring Sysmon with proper settings..." -ForegroundColor Green
    
    # Check if config file exists
    $configPath = ".\sysmon-config.xml"
    if (-not (Test-Path $configPath)) {
        Write-Host "         [ERROR] Configuration file not found: $configPath" -ForegroundColor Red
        Write-Host "         Make sure sysmon-config.xml is in the current directory" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "         Using configuration file: $configPath" -ForegroundColor White
    
    try {
        Write-Host "         Stopping Sysmon service..." -ForegroundColor White
        Stop-Service -Name Sysmon -Force -ErrorAction SilentlyContinue
        
        Write-Host "         Uninstalling current Sysmon..." -ForegroundColor White
        $result = sysmon -u force 2>&1
        Write-Host "         $result" -ForegroundColor Gray
        
        Start-Sleep -Seconds 2
        
        Write-Host "         Installing Sysmon with new configuration..." -ForegroundColor White
        $result = sysmon -accepteula -i $configPath 2>&1
        Write-Host "         $result" -ForegroundColor Gray
        
        Start-Sleep -Seconds 3
        
        # Verify installation
        $newService = Get-Service -Name Sysmon -ErrorAction SilentlyContinue
        if ($null -ne $newService -and $newService.Status -eq "Running") {
            Write-Host "         ? Sysmon reinstalled successfully" -ForegroundColor Green
        } else {
            Write-Host "         ? Sysmon installation may have failed" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "         [ERROR] Failed to reconfigure Sysmon: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "[Step 4] Final verification..." -ForegroundColor Green

# Final check
try {
    $finalLog = Get-WinEvent -ListLog Microsoft-Windows-Sysmon/Operational -ErrorAction Stop
    Write-Host "         ? Sysmon event log is now available!" -ForegroundColor Green
    Write-Host "         Log enabled: $($finalLog.IsEnabled)" -ForegroundColor White
    Write-Host "         Current record count: $($finalLog.RecordCount)" -ForegroundColor White
    
    # Check service status
    $finalService = Get-Service -Name Sysmon
    Write-Host "         Service status: $($finalService.Status)" -ForegroundColor White
    
    Write-Host ""
    Write-Host "===============================================" -ForegroundColor Green
    Write-Host "              ? SUCCESS!                      " -ForegroundColor Green
    Write-Host "===============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Sysmon is now properly configured and should generate events to:" -ForegroundColor White
    Write-Host "  ? Microsoft-Windows-Sysmon/Operational log" -ForegroundColor Gray
    Write-Host ""
    Write-Host "You can now:" -ForegroundColor Yellow
    Write-Host "  1. Run Test-EventGeneration.ps1 to generate test events" -ForegroundColor White
    Write-Host "  2. Start LogSentinel to see Sysmon events in the Events view" -ForegroundColor White
    Write-Host "  3. Filter events by Source = 'Sysmon'" -ForegroundColor White
    Write-Host ""
    
}
catch {
    Write-Host "         ? Sysmon event log still not available" -ForegroundColor Red
    Write-Host "         Error: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Manual steps to try:" -ForegroundColor Yellow
    Write-Host "  1. Run: sysmon -u force" -ForegroundColor White
    Write-Host "  2. Run: sysmon -accepteula -i sysmon-config.xml" -ForegroundColor White
    Write-Host "  3. Check Windows Event Viewer for Sysmon events" -ForegroundColor White
    Write-Host ""
}

Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")