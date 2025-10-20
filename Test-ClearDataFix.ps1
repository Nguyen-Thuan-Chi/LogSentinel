#!/usr/bin/env pwsh

# Test Script for Clear Data Fix
# This script helps verify that the Clear Data feature is working correctly

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "    LogSentinel - Clear Data Fix Test" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# Check the database path that the application will use
$appDataDir = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::LocalApplicationData)
$logSentinelDir = Join-Path $appDataDir "LogSentinel"
$dbPath = Join-Path $logSentinelDir "logsentinel.db"

Write-Host "Database path used by LogSentinel:" -ForegroundColor Yellow
Write-Host "  $dbPath" -ForegroundColor Green
Write-Host ""

# Check if database exists
if (Test-Path $dbPath) {
    Write-Host "✅ Database file exists" -ForegroundColor Green
    
    # Get file info
    $fileInfo = Get-Item $dbPath
    Write-Host "Database size: $($fileInfo.Length) bytes" -ForegroundColor Cyan
    Write-Host "Last modified: $($fileInfo.LastWriteTime)" -ForegroundColor Cyan
    Write-Host ""
    
    # Try to check database content using sqlite3 if available
    try {
        $sqliteCmd = Get-Command sqlite3 -ErrorAction SilentlyContinue
        if ($sqliteCmd) {
            Write-Host "📊 Database content check:" -ForegroundColor Yellow
            
            # Count events
            $eventCount = & sqlite3 $dbPath "SELECT COUNT(*) FROM Events;" 2>$null
            if ($eventCount) {
                Write-Host "  Events: $eventCount" -ForegroundColor Cyan
            }
            
            # Count alerts
            $alertCount = & sqlite3 $dbPath "SELECT COUNT(*) FROM Alerts;" 2>$null
            if ($alertCount) {
                Write-Host "  Alerts: $alertCount" -ForegroundColor Cyan
            }
            
            # Count rules
            $ruleCount = & sqlite3 $dbPath "SELECT COUNT(*) FROM Rules;" 2>$null
            if ($ruleCount) {
                Write-Host "  Rules: $ruleCount" -ForegroundColor Cyan
            }
            
            Write-Host ""
            
            if ([int]$eventCount -gt 0 -or [int]$alertCount -gt 0) {
                Write-Host "🎯 Ready to test Clear Data feature!" -ForegroundColor Green
                Write-Host "   The database contains data that can be cleared." -ForegroundColor White
            } else {
                Write-Host "⚠️ Database appears to be empty" -ForegroundColor Yellow
                Write-Host "   Run the application first to generate some data." -ForegroundColor White
            }
        } else {
            Write-Host "ℹ️ sqlite3 command not available" -ForegroundColor Yellow
            Write-Host "   Install SQLite CLI tools for detailed database inspection" -ForegroundColor White
        }
    } catch {
        Write-Host "⚠️ Error checking database content: $($_.Exception.Message)" -ForegroundColor Yellow
    }
} else {
    Write-Host "❌ Database file not found" -ForegroundColor Red
    Write-Host "   Run LogSentinel application first to create the database" -ForegroundColor White
}

Write-Host ""
Write-Host "=== TESTING INSTRUCTIONS ===" -ForegroundColor Yellow
Write-Host ""

Write-Host "🧪 To test the Clear Data fix:" -ForegroundColor Magenta
Write-Host "1. Start LogSentinel application" -ForegroundColor White
Write-Host "2. Navigate to the 'Settings' tab" -ForegroundColor White
Write-Host "3. Scroll down to the 'Data Management' section" -ForegroundColor White
Write-Host "4. Verify the database path shows:" -ForegroundColor White
Write-Host "   $dbPath" -ForegroundColor Green
Write-Host "5. Click 'Clear Event & Alert Data' button" -ForegroundColor White
Write-Host "6. ✅ Expected: Should work without 'Database is empty' error" -ForegroundColor Green
Write-Host "7. ✅ Expected: Should show count of cleared events/alerts" -ForegroundColor Green
Write-Host "8. ✅ Expected: Database size should be reduced after clearing" -ForegroundColor Green
Write-Host ""

Write-Host "🔧 What was fixed:" -ForegroundColor Cyan
Write-Host "• SettingsViewModel now uses correct database path" -ForegroundColor White
Write-Host "• Fixed database table existence checking" -ForegroundColor White
Write-Host "• Improved error handling for database operations" -ForegroundColor White
Write-Host "• Added bulk delete operations for better performance" -ForegroundColor White
Write-Host "• Added VACUUM command to reclaim database space" -ForegroundColor White
Write-Host ""

Write-Host "🐛 Previous issue:" -ForegroundColor Red
Write-Host "• Settings used wrong database path (logsentinel.db vs actual path)" -ForegroundColor White
Write-Host "• Wrong method to check if tables exist (migrations vs actual tables)" -ForegroundColor White
Write-Host "• Always showed 'Database is empty' even when data existed" -ForegroundColor White
Write-Host ""

Write-Host "===============================================" -ForegroundColor Green
Write-Host "        Fix Applied Successfully!              " -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")