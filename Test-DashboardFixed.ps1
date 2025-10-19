#!/usr/bin/env pwsh
# Test Dashboard v?i c?c mode kh?c nhau

Write-Host "=== DASHBOARD MODE TESTING - FIXED ===" -ForegroundColor Cyan
Write-Host "Testing Dashboard User Mode v? Professional Mode after OxyPlot fix"
Write-Host

# Function to test if process is running
function Test-ProcessRunning {
    param([string]$ProcessName)
    return (Get-Process -Name "LogSentinel.UI" -ErrorAction SilentlyContinue) -ne $null
}

# Function to kill process if running
function Stop-ProcessIfRunning {
    param([string]$ProcessName)
    Get-Process -Name "LogSentinel.UI" -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 2
}

try {
    # Stop any running instance
    Write-Host "1. Stopping existing Log Sentinel instances..." -ForegroundColor Yellow
    Stop-ProcessIfRunning "LogSentinel.UI"
    
    # Build project
    Write-Host "2. Building project..." -ForegroundColor Yellow
    dotnet build "Log Sentinel\LogSentinel.UI.csproj" --configuration Debug
    
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
    
    Write-Host "? Build successful" -ForegroundColor Green
    
    # Start application
    Write-Host "3. Starting Log Sentinel..." -ForegroundColor Yellow
    $process = Start-Process -FilePath "Log Sentinel\bin\Debug\net9.0-windows\LogSentinel.UI.exe" -PassThru
    
    if (-not $process) {
        throw "Failed to start application"
    }
    
    Write-Host "? Application started (PID: $($process.Id))" -ForegroundColor Green
    
    # Wait for startup
    Write-Host "4. Waiting for application to fully load..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5
    
    # Instructions for manual testing
    Write-Host
    Write-Host "=== MANUAL TESTING INSTRUCTIONS ===" -ForegroundColor Magenta
    Write-Host "1. Navigate to Dashboard tab" -ForegroundColor White
    Write-Host "2. Check that charts are displayed without errors (User Mode)" -ForegroundColor White
    Write-Host "3. Toggle 'Professional Mode' checkbox" -ForegroundColor White
    Write-Host "4. Verify that view switches to data tables" -ForegroundColor White
    Write-Host "5. Toggle back to User Mode" -ForegroundColor White
    Write-Host "6. Verify charts are shown again" -ForegroundColor White
    Write-Host "7. Click Refresh button" -ForegroundColor White
    Write-Host "8. Verify data updates without errors" -ForegroundColor White
    Write-Host
    
    Write-Host "=== EXPECTED RESULTS ===" -ForegroundColor Magenta
    Write-Host "? User Mode: Charts display correctly (Line chart + Pie chart)" -ForegroundColor Green
    Write-Host "? Professional Mode: Data tables display correctly" -ForegroundColor Green
    Write-Host "? No OxyPlot exceptions in output" -ForegroundColor Green
    Write-Host "? Smooth switching between modes" -ForegroundColor Green
    Write-Host "? Refresh button works without errors" -ForegroundColor Green
    Write-Host
    
    # Check if process is still running
    Start-Sleep -Seconds 3
    if (Test-ProcessRunning "LogSentinel.UI") {
        Write-Host "? Application is running stable" -ForegroundColor Green
    } else {
        Write-Host "? Application crashed after startup" -ForegroundColor Red
    }
    
    Write-Host
    Write-Host "=== TESTING NOTES ===" -ForegroundColor Yellow
    Write-Host "? OxyPlot error has been fixed by using LineSeries instead of BarSeries"
    Write-Host "? Charts should now display without CategoryAxis errors"
    Write-Host "? Professional mode shows data tables as expected"
    Write-Host "? Mode switching should be smooth and instant"
    Write-Host
    
    Read-Host "Press Enter to close the application and finish testing"
    
} catch {
    Write-Host "? Test failed: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    # Cleanup
    Write-Host "5. Cleaning up..." -ForegroundColor Yellow
    Stop-ProcessIfRunning "LogSentinel.UI"
    Write-Host "? Cleanup completed" -ForegroundColor Green
}

Write-Host
Write-Host "=== DASHBOARD TESTING COMPLETED ===" -ForegroundColor Cyan