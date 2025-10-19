#!/usr/bin/env pwsh

# Test Alert System Script for LogSentinel
# This script generates events that should trigger alerts

Write-Host "=== LogSentinel Alert System Test ===" -ForegroundColor Cyan

# Test 1: Notepad Launch (should trigger Rule ID 1)
Write-Host "`n?? Test 1: Testing Notepad Launch Alert" -ForegroundColor Yellow

$testEvent1 = @"
{
  "EventTime": "$(Get-Date -Format "yyyy-MM-ddTHH:mm:ss.fffZ")",
  "Host": "TESTPC",
  "User": "testuser",
  "EventId": 1,
  "Provider": "Microsoft-Windows-Sysmon",
  "Level": "Info",
  "Process": "notepad.exe",
  "ParentProcess": "explorer.exe",
  "Action": "ProcessCreate",
  "Object": "C:\\Windows\\System32\\notepad.exe",
  "DetailsJson": "{\"Image\":\"C:\\\\Windows\\\\System32\\\\notepad.exe\",\"ProcessId\":1234,\"User\":\"TESTPC\\\\testuser\",\"ParentImage\":\"C:\\\\Windows\\\\explorer.exe\"}",
  "Source": "Sysmon"
}
"@

# Test 2: PowerShell with suspicious command
Write-Host "`n?? Test 2: Testing Suspicious PowerShell Alert" -ForegroundColor Yellow

$testEvent2 = @"
{
  "EventTime": "$(Get-Date -Format "yyyy-MM-ddTHH:mm:ss.fffZ")",
  "Host": "TESTPC",
  "User": "testuser",
  "EventId": 1,
  "Provider": "Microsoft-Windows-Sysmon",
  "Level": "Info", 
  "Process": "powershell.exe",
  "ParentProcess": "cmd.exe",
  "Action": "ProcessCreate",
  "Object": "C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe",
  "DetailsJson": "{\"Image\":\"C:\\\\Windows\\\\System32\\\\WindowsPowerShell\\\\v1.0\\\\powershell.exe\",\"CommandLine\":\"powershell.exe -NoP -W Hidden -Enc aGVsbG8=\",\"ProcessId\":5678}",
  "Source": "Sysmon"
}
"@

# Test 3: Multiple Failed Logins (should trigger threshold rule)
Write-Host "`n?? Test 3: Testing Failed Login Threshold Alert" -ForegroundColor Yellow

$failedLoginEvents = @()
for ($i = 1; $i -le 6; $i++) {
    $failedLoginEvent = @"
{
  "EventTime": "$(Get-Date -Format "yyyy-MM-ddTHH:mm:ss.fffZ")",
  "Host": "TESTPC",
  "User": "alice",
  "EventId": 4625,
  "Provider": "Microsoft-Windows-Security",
  "Level": "Warning",
  "Process": "winlogon.exe",
  "Action": "Logon",
  "Object": "Security",
  "DetailsJson": "{\"LogonType\":2,\"Status\":\"0xC000006D\",\"SubStatus\":\"0xC0000064\",\"TargetUserName\":\"alice\",\"WorkstationName\":\"TESTPC\"}",
  "Source": "WindowsEventLog"
}
"@
    $failedLoginEvents += $failedLoginEvent
}

# Create sample-logs directory if it doesn't exist
$sampleLogsPath = "./sample-logs/incoming"
if (-not (Test-Path $sampleLogsPath)) {
    New-Item -ItemType Directory -Path $sampleLogsPath -Force | Out-Null
    Write-Host "? Created sample-logs directory" -ForegroundColor Green
}

# Write test events to files
Write-Host "`n?? Creating test log files..." -ForegroundColor Cyan

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

# Test 1: Notepad event
$testFile1 = "$sampleLogsPath/test_notepad_$timestamp.log"
$testEvent1 | Out-File -FilePath $testFile1 -Encoding UTF8
Write-Host "? Created: $testFile1" -ForegroundColor Green

Start-Sleep -Seconds 1

# Test 2: Suspicious PowerShell event
$testFile2 = "$sampleLogsPath/test_powershell_$timestamp.log"
$testEvent2 | Out-File -FilePath $testFile2 -Encoding UTF8
Write-Host "? Created: $testFile2" -ForegroundColor Green

Start-Sleep -Seconds 1

# Test 3: Failed login events (write all to one file)
$testFile3 = "$sampleLogsPath/test_failed_logins_$timestamp.log"
$failedLoginEvents -join "`n" | Out-File -FilePath $testFile3 -Encoding UTF8
Write-Host "? Created: $testFile3 (6 failed login events)" -ForegroundColor Green

Write-Host "`n?? Test files created successfully!" -ForegroundColor Green
Write-Host "These files should be automatically processed by LogSentinel if it's running." -ForegroundColor Yellow
Write-Host "`nExpected results:" -ForegroundColor Cyan
Write-Host "- Alert for Notepad launch (Rule: Notepad Launch Detection)" -ForegroundColor White
Write-Host "- Alert for suspicious PowerShell command (Rule: Suspicious PowerShell)" -ForegroundColor White
Write-Host "- Alert for multiple failed logins (Rule: Failed Login Threshold)" -ForegroundColor White

Write-Host "`nTo verify:" -ForegroundColor Cyan
Write-Host "1. Check the Alert tab in LogSentinel" -ForegroundColor White
Write-Host "2. Check the application logs for alert creation messages" -ForegroundColor White
Write-Host "3. Run Test-AlertDatabase.ps1 to check the database" -ForegroundColor White

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan