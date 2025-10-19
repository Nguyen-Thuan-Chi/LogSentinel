#!/usr/bin/env pwsh

# Simple Alert Database Check using dotnet sqlite
Write-Host "=== LogSentinel Alert Database Check ===" -ForegroundColor Cyan

$dbPath = "$env:LOCALAPPDATA\LogSentinel\logsentinel.db"
Write-Host "Database path: $dbPath" -ForegroundColor Yellow

if (Test-Path $dbPath) {
    Write-Host "? Database file exists" -ForegroundColor Green
    
    # Try using sqlite3 command if available
    try {
        $sqliteCmd = Get-Command sqlite3 -ErrorAction SilentlyContinue
        if ($sqliteCmd) {
            Write-Host "`nChecking tables in database..." -ForegroundColor Yellow
            
            # List tables
            $tables = & sqlite3 $dbPath ".tables"
            Write-Host "Tables: $tables" -ForegroundColor Cyan
            
            # Count alerts
            $alertCount = & sqlite3 $dbPath "SELECT COUNT(*) FROM Alerts;"
            Write-Host "Alert count: $alertCount" -ForegroundColor Cyan
            
            # Count rules
            $ruleCount = & sqlite3 $dbPath "SELECT COUNT(*) FROM Rules;"
            Write-Host "Rule count: $ruleCount" -ForegroundColor Cyan
            
            # Show recent alerts if any
            if ([int]$alertCount -gt 0) {
                Write-Host "`nRecent alerts:" -ForegroundColor Yellow
                & sqlite3 $dbPath "SELECT Id, RuleName, Severity, Title, datetime(Timestamp) FROM Alerts ORDER BY Id DESC LIMIT 5;" | ForEach-Object {
                    Write-Host "  $_" -ForegroundColor White
                }
            }
            
            # Show active rules
            if ([int]$ruleCount -gt 0) {
                Write-Host "`nActive rules:" -ForegroundColor Yellow
                & sqlite3 $dbPath "SELECT Name, Severity FROM Rules WHERE IsEnabled = 1;" | ForEach-Object {
                    Write-Host "  $_" -ForegroundColor White
                }
            }
            
        } else {
            Write-Host "sqlite3 command not found. Install SQLite CLI tools for detailed database inspection." -ForegroundColor Yellow
        }
    } catch {
        Write-Host "Error using sqlite3: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Check file size and modification time
    $dbInfo = Get-Item $dbPath
    Write-Host "`nDatabase file info:" -ForegroundColor Yellow
    Write-Host "  Size: $($dbInfo.Length) bytes" -ForegroundColor Cyan
    Write-Host "  Last modified: $($dbInfo.LastWriteTime)" -ForegroundColor Cyan
    
} else {
    Write-Host "? Database file does not exist at: $dbPath" -ForegroundColor Red
    Write-Host "The LogSentinel application may not have run yet or database creation failed." -ForegroundColor Yellow
}

Write-Host "`n=== Next Steps ===" -ForegroundColor Cyan
Write-Host "1. Start LogSentinel application" -ForegroundColor White
Write-Host "2. Navigate to Alerts view" -ForegroundColor White  
Write-Host "3. Check if alerts are visible" -ForegroundColor White
Write-Host "4. If no alerts, check application logs for errors" -ForegroundColor White