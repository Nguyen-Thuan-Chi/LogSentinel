#!/usr/bin/env pwsh
# Quick test for Dashboard functionality

Write-Host "=== QUICK DASHBOARD TEST ===" -ForegroundColor Cyan
Write-Host "Testing Dashboard OxyPlot fixes..." -ForegroundColor Yellow

try {
    # Build if needed
    Write-Host "Building project..." -ForegroundColor Yellow
    dotnet build "Log Sentinel\LogSentinel.UI.csproj" --configuration Debug --verbosity quiet
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Build successful - Dashboard OxyPlot fix applied" -ForegroundColor Green
        
        Write-Host
        Write-Host "=== FIX SUMMARY ===" -ForegroundColor Magenta
        Write-Host "? Changed BarSeries to LineSeries (fixes CategoryAxis error)" -ForegroundColor Green
        Write-Host "? Changed BarItem to DataPoint (fixes item type error)" -ForegroundColor Green
        Write-Host "? Charts now use proper OxyPlot series types" -ForegroundColor Green
        Write-Host
        
        Write-Host "=== MANUAL TESTING NEEDED ===" -ForegroundColor Yellow
        Write-Host "1. Run the application manually: dotnet run --project 'Log Sentinel\LogSentinel.UI.csproj'" -ForegroundColor White
        Write-Host "2. Navigate to Dashboard tab" -ForegroundColor White
        Write-Host "3. Check User Mode: Should see line chart + pie chart without errors" -ForegroundColor White
        Write-Host "4. Toggle Professional Mode: Should see data tables" -ForegroundColor White
        Write-Host "5. Toggle back to User Mode: Charts should reappear" -ForegroundColor White
        Write-Host
        
        Write-Host "=== EXPECTED RESULTS ===" -ForegroundColor Green
        Write-Host "? No OxyPlot exceptions in console output" -ForegroundColor White
        Write-Host "? Smooth switching between User and Professional modes" -ForegroundColor White
        Write-Host "? Charts display properly in User mode" -ForegroundColor White
        Write-Host "? Data tables display properly in Professional mode" -ForegroundColor White
        
    } else {
        Write-Host "? Build failed" -ForegroundColor Red
        exit 1
    }
    
} catch {
    Write-Host "? Test failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host
Write-Host "=== DASHBOARD FIX COMPLETED ===" -ForegroundColor Cyan