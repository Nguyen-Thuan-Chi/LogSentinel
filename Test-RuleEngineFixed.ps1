#!/usr/bin/env pwsh

# Test Rule Engine After Fix Script
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "    LogSentinel - Rule Engine Fix Test        " -ForegroundColor Cyan  
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "?? FIXES APPLIED:" -ForegroundColor Green
Write-Host "? Removed excessive Debug.WriteLine() calls causing lag" -ForegroundColor White
Write-Host "? Fixed EvaluateEventAsync to properly implement rule logic" -ForegroundColor White
Write-Host "? Updated rules to match log_source only when no detection section" -ForegroundColor White
Write-Host "? Added 'Any Sysmon Process Creation' rule for testing" -ForegroundColor White
Write-Host "? Simplified rule YAML format in SeedData" -ForegroundColor White
Write-Host ""

Write-Host "?? EXPECTED BEHAVIOR:" -ForegroundColor Yellow
Write-Host "? Rule engine should no longer lag the application" -ForegroundColor White
Write-Host "? Any Sysmon Event ID 1 should trigger 'Any Sysmon Process Creation' rule" -ForegroundColor White
Write-Host "? Specific process paths should trigger specific rules" -ForegroundColor White
Write-Host "? Alerts should appear in Alerts view and Dashboard" -ForegroundColor White
Write-Host ""

Write-Host "?? DATABASE RULES:" -ForegroundColor Cyan
Write-Host "1. Notepad Execution (matches C:\Windows\System32\notepad.exe)" -ForegroundColor Gray
Write-Host "2. Calculator Execution (matches C:\Windows\System32\calc.exe)" -ForegroundColor Gray
Write-Host "3. PowerShell Process Creation (matches powershell.exe)" -ForegroundColor Gray
Write-Host "4. Command Prompt Execution (matches C:\Windows\System32\cmd.exe)" -ForegroundColor Gray
Write-Host "5. Failed Login Attempts (matches Event ID 4625)" -ForegroundColor Gray
Write-Host "6. Any Sysmon Process Creation (matches ANY Sysmon Event ID 1)" -ForegroundColor Gray
Write-Host ""

Write-Host "?? TESTING INSTRUCTIONS:" -ForegroundColor Yellow
Write-Host "1. Start LogSentinel (should no longer lag)" -ForegroundColor White
Write-Host "2. Run this to generate test events:" -ForegroundColor White
Write-Host "   Start-Process notepad.exe -PassThru | Stop-Process" -ForegroundColor Gray
Write-Host "   Start-Process calc.exe -PassThru | Stop-Process" -ForegroundColor Gray
Write-Host "3. Check Alerts view - should see new alerts" -ForegroundColor White
Write-Host "4. Check Dashboard - alert counts should increase" -ForegroundColor White
Write-Host "5. Check Rules view - trigger counts should increase" -ForegroundColor White
Write-Host ""

Write-Host "?? PERFORMANCE IMPROVEMENTS:" -ForegroundColor Green
Write-Host "? Removed ~20 Debug.WriteLine() calls per event" -ForegroundColor White
Write-Host "? Streamlined rule matching logic" -ForegroundColor White
Write-Host "? Better error handling and logging" -ForegroundColor White
Write-Host ""

Write-Host "?? TROUBLESHOOTING:" -ForegroundColor Magenta
Write-Host "If alerts still don't appear:" -ForegroundColor Yellow
Write-Host "1. Check if Sysmon is installed and running" -ForegroundColor White
Write-Host "2. Verify 'Sysmon' source is enabled in appsettings.json" -ForegroundColor White
Write-Host "3. Run as Administrator for better event access" -ForegroundColor White
Write-Host "4. Check Output window in LogSentinel for rule engine messages" -ForegroundColor White
Write-Host ""

# Function to test process creation
function Test-ProcessCreation {
    Write-Host "?? Creating test processes to trigger rules..." -ForegroundColor Yellow
    
    try {
        Write-Host "  Starting notepad.exe..." -ForegroundColor Gray
        $notepad = Start-Process notepad.exe -PassThru -WindowStyle Hidden
        Start-Sleep -Milliseconds 500
        Stop-Process -Id $notepad.Id -Force
        Write-Host "  ? notepad.exe process created and stopped" -ForegroundColor Green
    } catch {
        Write-Host "  ? Failed to create notepad process: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Start-Sleep -Milliseconds 300
    
    try {
        Write-Host "  Starting calc.exe..." -ForegroundColor Gray
        $calc = Start-Process calc.exe -PassThru -WindowStyle Hidden
        Start-Sleep -Milliseconds 500
        Stop-Process -Id $calc.Id -Force
        Write-Host "  ? calc.exe process created and stopped" -ForegroundColor Green
    } catch {
        Write-Host "  ? Failed to create calc process: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host "  ? Test events generated - check LogSentinel for alerts!" -ForegroundColor Cyan
}

Write-Host "?? OPTIONS:" -ForegroundColor Cyan
Write-Host "1. Press [T] to generate test events now" -ForegroundColor White
Write-Host "2. Press [R] to run LogSentinel" -ForegroundColor White
Write-Host "3. Press [Q] to quit" -ForegroundColor White
Write-Host ""

do {
    $key = Read-Host "Choose option (T/R/Q)"
    switch ($key.ToUpper()) {
        "T" {
            Test-ProcessCreation
            Write-Host ""
        }
        "R" {
            Write-Host "Starting LogSentinel..." -ForegroundColor Green
            Set-Location "Log Sentinel"
            try {
                dotnet run
            } catch {
                Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
            } finally {
                Set-Location ".."
            }
        }
        "Q" {
            Write-Host "Exiting..." -ForegroundColor Yellow
            break
        }
        default {
            Write-Host "Invalid option. Choose T, R, or Q." -ForegroundColor Red
        }
    }
} while ($key.ToUpper() -ne "Q")

Write-Host ""
Write-Host "===============================================" -ForegroundColor Green
Write-Host "         Rule Engine Fix Test Complete!       " -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green