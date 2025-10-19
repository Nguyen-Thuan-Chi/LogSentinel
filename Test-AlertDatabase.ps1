#!/usr/bin/env pwsh

# Test Alert Database Script for LogSentinel
# This script checks the database for alerts, rules, and events

Write-Host "=== LogSentinel Alert Database Test ===" -ForegroundColor Cyan

# Define database path
$dbPath = "$env:LOCALAPPDATA\LogSentinel\logsentinel.db"
Write-Host "Database path: $dbPath" -ForegroundColor Yellow

if (-not (Test-Path $dbPath)) {
    Write-Host "? Database not found at $dbPath" -ForegroundColor Red
    Write-Host "Please run the application first to create the database." -ForegroundColor Yellow
    exit 1
}

# Check if sqlite3 is available
$sqlite3Path = Get-Command sqlite3 -ErrorAction SilentlyContinue
if (-not $sqlite3Path) {
    Write-Host "? sqlite3 command not found." -ForegroundColor Red
    Write-Host "Please install SQLite CLI tools or use a database browser." -ForegroundColor Yellow
    exit 1
}

Write-Host "? Database file exists" -ForegroundColor Green
Write-Host "? SQLite CLI available" -ForegroundColor Green

try {
    # Check tables
    Write-Host "`n--- Database Tables ---" -ForegroundColor Cyan
    $tables = sqlite3 $dbPath ".tables"
    Write-Host $tables

    # Check Rules
    Write-Host "`n--- Rules Count ---" -ForegroundColor Cyan
    $rulesCount = sqlite3 $dbPath "SELECT COUNT(*) FROM Rules;"
    Write-Host "Total Rules: $rulesCount" -ForegroundColor Yellow

    if ([int]$rulesCount -gt 0) {
        Write-Host "`n--- Rules Details ---" -ForegroundColor Cyan
        $rules = sqlite3 $dbPath "SELECT Id, Name, Severity, IsEnabled, TriggerCount FROM Rules;"
        Write-Host $rules
    } else {
        Write-Host "??  No rules found in database" -ForegroundColor Yellow
    }

    # Check Events
    Write-Host "`n--- Events Count ---" -ForegroundColor Cyan
    $eventsCount = sqlite3 $dbPath "SELECT COUNT(*) FROM Events;"
    Write-Host "Total Events: $eventsCount" -ForegroundColor Yellow

    if ([int]$eventsCount -gt 0) {
        Write-Host "`n--- Recent Events (Last 5) ---" -ForegroundColor Cyan
        $recentEvents = sqlite3 $dbPath "SELECT Id, EventTime, EventId, Level, Action, Host, User FROM Events ORDER BY Id DESC LIMIT 5;"
        Write-Host $recentEvents
    } else {
        Write-Host "??  No events found in database" -ForegroundColor Yellow
    }

    # Check Alerts
    Write-Host "`n--- Alerts Count ---" -ForegroundColor Cyan
    $alertsCount = sqlite3 $dbPath "SELECT COUNT(*) FROM Alerts;"
    Write-Host "Total Alerts: $alertsCount" -ForegroundColor Yellow

    if ([int]$alertsCount -gt 0) {
        Write-Host "`n--- Alerts Details ---" -ForegroundColor Cyan
        $alerts = sqlite3 $dbPath "SELECT Id, RuleName, Severity, Timestamp, Title, IsAcknowledged FROM Alerts ORDER BY Timestamp DESC;"
        Write-Host $alerts
    } else {
        Write-Host "??  No alerts found in database" -ForegroundColor Yellow
        Write-Host "This explains why alerts are not showing up!" -ForegroundColor Red
    }

    # Check for specific Notepad events
    Write-Host "`n--- Notepad Events ---" -ForegroundColor Cyan
    $notepadEvents = sqlite3 $dbPath "SELECT COUNT(*) FROM Events WHERE Process LIKE '%notepad%' OR Action LIKE '%notepad%' OR DetailsJson LIKE '%notepad%';"
    Write-Host "Notepad-related Events: $notepadEvents" -ForegroundColor Yellow

    if ([int]$notepadEvents -gt 0) {
        Write-Host "`n--- Notepad Event Details ---" -ForegroundColor Cyan
        $notepadDetails = sqlite3 $dbPath "SELECT Id, EventTime, EventId, Process, Action, DetailsJson FROM Events WHERE Process LIKE '%notepad%' OR Action LIKE '%notepad%' OR DetailsJson LIKE '%notepad%' LIMIT 5;"
        Write-Host $notepadDetails
    }

    # Check if Rule 1 (Notepad rule) exists and has been triggered
    Write-Host "`n--- Rule ID 1 Status ---" -ForegroundColor Cyan
    $rule1 = sqlite3 $dbPath "SELECT Id, Name, Severity, IsEnabled, TriggerCount, LastTriggeredAt FROM Rules WHERE Id = 1;"
    if ($rule1) {
        Write-Host "Rule 1: $rule1" -ForegroundColor Yellow
    } else {
        Write-Host "??  Rule ID 1 not found" -ForegroundColor Yellow
    }

} catch {
    Write-Host "? Error querying database: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan