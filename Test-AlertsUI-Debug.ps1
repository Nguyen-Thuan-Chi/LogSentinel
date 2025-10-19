#!/usr/bin/env pwsh

# Alert UI Debug Test Script
# Tests alert creation, database storage, and UI loading

Write-Host "=== LogSentinel Alert UI Debug Test ===" -ForegroundColor Cyan

# Test 1: Check database connection and table structure
Write-Host "`n1. Checking database structure..." -ForegroundColor Yellow

$dbPath = "$env:LOCALAPPDATA\LogSentinel\logsentinel.db"
Write-Host "Database path: $dbPath"

if (Test-Path $dbPath) {
    Write-Host "? Database file exists" -ForegroundColor Green
    
    # Connect to SQLite and check tables
    try {
        Add-Type -Path "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Data.SQLite.dll" -ErrorAction SilentlyContinue
        
        $connectionString = "Data Source=$dbPath"
        $connection = New-Object System.Data.SQLite.SQLiteConnection($connectionString)
        $connection.Open()
        
        # Check if Alerts table exists
        $command = $connection.CreateCommand()
        $command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Alerts'"
        $result = $command.ExecuteScalar()
        
        if ($result) {
            Write-Host "? Alerts table exists" -ForegroundColor Green
            
            # Check table structure
            $command.CommandText = "PRAGMA table_info(Alerts)"
            $reader = $command.ExecuteReader()
            Write-Host "Alert table columns:"
            while ($reader.Read()) {
                $colName = $reader["name"]
                $colType = $reader["type"]
                Write-Host "  - $colName ($colType)" -ForegroundColor Gray
            }
            $reader.Close()
            
            # Count existing alerts
            $command.CommandText = "SELECT COUNT(*) FROM Alerts"
            $alertCount = $command.ExecuteScalar()
            Write-Host "Current alerts in database: $alertCount" -ForegroundColor Cyan
            
            # Show recent alerts if any exist
            if ($alertCount -gt 0) {
                Write-Host "`nRecent alerts:" -ForegroundColor Yellow
                $command.CommandText = "SELECT Id, RuleName, Severity, Timestamp, Title FROM Alerts ORDER BY Timestamp DESC LIMIT 5"
                $reader = $command.ExecuteReader()
                while ($reader.Read()) {
                    $id = $reader["Id"]
                    $rule = $reader["RuleName"]
                    $severity = $reader["Severity"]
                    $timestamp = $reader["Timestamp"]
                    $title = $reader["Title"]
                    Write-Host "  Alert $id - [$severity] $rule - $title ($timestamp)" -ForegroundColor White
                }
                $reader.Close()
            }
        } else {
            Write-Host "? Alerts table does not exist" -ForegroundColor Red
        }
        
        $connection.Close()
    } catch {
        Write-Host "? Error connecting to database: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "? Database file does not exist at: $dbPath" -ForegroundColor Red
}

# Test 2: Check if Rules exist (needed to create alerts)
Write-Host "`n2. Checking for rules..." -ForegroundColor Yellow

try {
    $connection = New-Object System.Data.SQLite.SQLiteConnection($connectionString)
    $connection.Open()
    
    $command = $connection.CreateCommand()
    $command.CommandText = "SELECT COUNT(*) FROM Rules WHERE IsEnabled = 1"
    $ruleCount = $command.ExecuteScalar()
    Write-Host "Active rules in database: $ruleCount" -ForegroundColor Cyan
    
    if ($ruleCount -gt 0) {
        Write-Host "Active rules:" -ForegroundColor Yellow
        $command.CommandText = "SELECT Name, Severity FROM Rules WHERE IsEnabled = 1"
        $reader = $command.ExecuteReader()
        while ($reader.Read()) {
            $name = $reader["Name"]
            $severity = $reader["Severity"]
            Write-Host "  - $name ($severity)" -ForegroundColor White
        }
        $reader.Close()
    }
    
    $connection.Close()
} catch {
    Write-Host "? Error checking rules: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Create a manual test alert
Write-Host "`n3. Creating test alert..." -ForegroundColor Yellow

try {
    $connection = New-Object System.Data.SQLite.SQLiteConnection($connectionString)
    $connection.Open()
    
    # First check if we have any rules
    $command = $connection.CreateCommand()
    $command.CommandText = "SELECT Id, Name FROM Rules WHERE IsEnabled = 1 LIMIT 1"
    $reader = $command.ExecuteReader()
    
    $ruleId = $null
    $ruleName = $null
    
    if ($reader.Read()) {
        $ruleId = $reader["Id"]
        $ruleName = $reader["Name"]
    }
    $reader.Close()
    
    if ($ruleId) {
        # Create a test alert
        $currentTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"
        $command.CommandText = @"
INSERT INTO Alerts (RuleId, RuleName, Severity, Timestamp, Title, Description, EventIdsJson, MetadataJson, IsAcknowledged)
VALUES (@RuleId, @RuleName, 'High', @Timestamp, 'Test Alert from PowerShell', 'This is a test alert created by the debug script', '[]', '{}', 0)
"@
        $command.Parameters.AddWithValue("@RuleId", $ruleId)
        $command.Parameters.AddWithValue("@RuleName", $ruleName)
        $command.Parameters.AddWithValue("@Timestamp", $currentTime)
        
        $result = $command.ExecuteNonQuery()
        
        if ($result -eq 1) {
            Write-Host "? Test alert created successfully" -ForegroundColor Green
            
            # Get the new alert ID
            $command.CommandText = "SELECT last_insert_rowid()"
            $newAlertId = $command.ExecuteScalar()
            Write-Host "New alert ID: $newAlertId" -ForegroundColor Cyan
        } else {
            Write-Host "? Failed to create test alert" -ForegroundColor Red
        }
    } else {
        Write-Host "? No active rules found - cannot create test alert" -ForegroundColor Red
    }
    
    $connection.Close()
} catch {
    Write-Host "? Error creating test alert: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Verify total alert count after test
Write-Host "`n4. Verifying alert count..." -ForegroundColor Yellow

try {
    $connection = New-Object System.Data.SQLite.SQLiteConnection($connectionString)
    $connection.Open()
    
    $command = $connection.CreateCommand()
    $command.CommandText = "SELECT COUNT(*) FROM Alerts"
    $finalAlertCount = $command.ExecuteScalar()
    Write-Host "Total alerts after test: $finalAlertCount" -ForegroundColor Cyan
    
    # Show most recent alert
    $command.CommandText = "SELECT Id, RuleName, Severity, Title, Timestamp FROM Alerts ORDER BY Id DESC LIMIT 1"
    $reader = $command.ExecuteReader()
    if ($reader.Read()) {
        $id = $reader["Id"]
        $rule = $reader["RuleName"]
        $severity = $reader["Severity"]
        $title = $reader["Title"]
        $timestamp = $reader["Timestamp"]
        Write-Host "Most recent alert: #$id - [$severity] $rule - $title ($timestamp)" -ForegroundColor White
    }
    $reader.Close()
    
    $connection.Close()
} catch {
    Write-Host "? Error verifying alerts: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
Write-Host "Now check the LogSentinel application UI to see if alerts appear in the Alerts view." -ForegroundColor Yellow
Write-Host "If alerts don't appear, the issue is likely in the UI binding or data loading logic." -ForegroundColor Yellow