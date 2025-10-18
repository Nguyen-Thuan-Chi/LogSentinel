# Test Database Connection Script for LogSentinel
Write-Host "Testing LogSentinel Database Connection..." -ForegroundColor Green

# Navigate to the Log Sentinel directory
$logSentinelPath = "Log Sentinel"
if (Test-Path $logSentinelPath) {
    Set-Location $logSentinelPath
    Write-Host "Changed directory to: $logSentinelPath" -ForegroundColor Yellow
} else {
    Write-Host "Log Sentinel directory not found!" -ForegroundColor Red
    exit 1
}

# Ensure data directory exists
$dataDir = "data"
if (!(Test-Path $dataDir)) {
    New-Item -ItemType Directory -Force -Path $dataDir | Out-Null
    Write-Host "Created data directory" -ForegroundColor Yellow
}

# Check if database file will be created in the right place
$dbPath = Join-Path $dataDir "logsentinel.db"
Write-Host "Database will be created at: $dbPath" -ForegroundColor Cyan

# Display connection strings
Write-Host "`nConnection Strings:" -ForegroundColor Green
Write-Host "Production (appsettings.json):" -ForegroundColor Yellow
if (Test-Path "appsettings.json") {
    $prodSettings = Get-Content "appsettings.json" | ConvertFrom-Json
    Write-Host "  $($prodSettings.ConnectionStrings.DefaultConnection)" -ForegroundColor White
}

Write-Host "Development (appsettings.Development.json):" -ForegroundColor Yellow
if (Test-Path "appsettings.Development.json") {
    $devSettings = Get-Content "appsettings.Development.json" | ConvertFrom-Json
    Write-Host "  $($devSettings.ConnectionStrings.DefaultConnection)" -ForegroundColor White
}

# Try to run the application
Write-Host "`nStarting LogSentinel application..." -ForegroundColor Green
Write-Host "Press Ctrl+C to stop the application" -ForegroundColor Yellow

try {
    dotnet run --project "LogSentinel.UI.csproj"
} catch {
    Write-Host "Error running application: $($_.Exception.Message)" -ForegroundColor Red
}

# Go back to original directory
Set-Location ..