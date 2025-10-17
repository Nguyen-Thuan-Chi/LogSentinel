# Generate synthetic log events for testing
param(
    [int]$Count = 1000,
    [string]$OutputPath = "sample-logs/incoming/generated-events.log"
)

$hosts = @("WEB-SERVER-01", "DB-SERVER-01", "APP-SERVER-01", "DC-01", "WORKSTATION-42")
$users = @("alice", "bob", "charlie", "dave", "eve", "admin", "service_account", "guest")
$processes = @("svchost.exe", "chrome.exe", "powershell.exe", "cmd.exe", "explorer.exe", "sqlservr.exe", "w3wp.exe", "lsass.exe")
$levels = @("INFO", "INFO", "INFO", "INFO", "WARNING", "ERROR")
$messages = @(
    "Application started successfully",
    "User logged in",
    "File access granted",
    "Service started",
    "Configuration updated",
    "Memory usage is high",
    "Disk space running low",
    "Connection timeout",
    "Failed to connect to database",
    "Authentication failed",
    "Access denied",
    "Service crashed unexpectedly",
    "Database connection established",
    "Certificate validation failed",
    "Network connection lost",
    "Backup completed successfully"
)

$startDate = (Get-Date).AddDays(-7)
$logLines = @()

Write-Host "Generating $Count log events..." -ForegroundColor Green

for ($i = 0; $i -lt $Count; $i++) {
    $timestamp = $startDate.AddMinutes((Get-Random -Minimum 0 -Maximum 10080))
    $host = $hosts | Get-Random
    $user = $users | Get-Random
    $process = $processes | Get-Random
    $level = $levels | Get-Random
    $message = $messages | Get-Random
    
    $timestampStr = $timestamp.ToString("yyyy-MM-dd HH:mm:ss")
    $logLine = "$timestampStr [$level] $host $user $process $message"
    $logLines += $logLine
    
    if (($i + 1) % 100 -eq 0) {
        Write-Host "Generated $($i + 1) events..." -ForegroundColor Cyan
    }
}

# Sort by timestamp
$logLines = $logLines | Sort-Object

# Ensure directory exists
$dir = Split-Path -Parent $OutputPath
if (!(Test-Path $dir)) {
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
}

# Write to file
$logLines | Out-File -FilePath $OutputPath -Encoding UTF8

Write-Host "Successfully generated $Count events to $OutputPath" -ForegroundColor Green
Write-Host "File size: $([math]::Round((Get-Item $OutputPath).Length / 1KB, 2)) KB" -ForegroundColor Yellow
