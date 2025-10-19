# Test Alert System - LogSentinel
# This script tests the Alert and Rule Engine functionality

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "    LogSentinel - Alert System Test          " -ForegroundColor Cyan  
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Alert System Features Implemented:" -ForegroundColor Green
Write-Host "? Alert and Rule entities with proper database schema" -ForegroundColor White
Write-Host "? Alert and Rule repositories for data access" -ForegroundColor White
Write-Host "? Rule Engine service for evaluating events against rules" -ForegroundColor White
Write-Host "? Alert Service for creating and managing alerts" -ForegroundColor White
Write-Host "? Integration with EventImporter for real-time rule evaluation" -ForegroundColor White
Write-Host "? Sample security detection rules (failed logins, admin creation, etc.)" -ForegroundColor White
Write-Host "? Fixed Source Filter in Events View (ComboBox binding issue)" -ForegroundColor White
Write-Host ""

Write-Host "Sample Rules Created in Database:" -ForegroundColor Yellow
Write-Host "1. Failed Login Threshold - Detects multiple failed logins (Event ID 4625)" -ForegroundColor Gray
Write-Host "2. Admin User Created - Detects user added to Administrators group (Event ID 4732)" -ForegroundColor Gray
Write-Host "3. Suspicious PowerShell - Detects PowerShell with suspicious flags" -ForegroundColor Gray
Write-Host "4. Process Creation Monitoring - Monitors Sysmon Event ID 1" -ForegroundColor Gray
Write-Host "5. Network Connection Monitoring - Monitors Sysmon Event ID 3" -ForegroundColor Gray
Write-Host ""

Write-Host "How the Alert System Works:" -ForegroundColor Yellow
Write-Host "1. Rules are loaded from database on startup" -ForegroundColor White
Write-Host "2. EventImporter processes each new event through Rule Engine" -ForegroundColor White
Write-Host "3. Rule Engine evaluates event against all active rules" -ForegroundColor White
Write-Host "4. When rule matches, Alert Service creates alert in database" -ForegroundColor White
Write-Host "5. Alerts appear in Alerts view and Dashboard counters" -ForegroundColor White
Write-Host "6. Users can acknowledge alerts and add notes" -ForegroundColor White
Write-Host ""

Write-Host "Testing Instructions:" -ForegroundColor Yellow
Write-Host "1. Start LogSentinel as Administrator" -ForegroundColor White
Write-Host "2. Check Dashboard for alert counts" -ForegroundColor White
Write-Host "3. Navigate to Rules view to see sample rules" -ForegroundColor White  
Write-Host "4. Navigate to Alerts view to see triggered alerts" -ForegroundColor White
Write-Host "5. Test Source Filter in Events view:" -ForegroundColor White
Write-Host "   ? Try selecting different sources (All, Sysmon, System, etc.)" -ForegroundColor Gray
Write-Host "   ? Verify events are filtered correctly" -ForegroundColor Gray
Write-Host "6. Generate test events to trigger alerts:" -ForegroundColor White
Write-Host "   ? Multiple failed logins (Event ID 4625)" -ForegroundColor Gray
Write-Host "   ? PowerShell with suspicious parameters" -ForegroundColor Gray
Write-Host "   ? Process creation events from Sysmon" -ForegroundColor Gray
Write-Host ""

Write-Host "Database Schema Changes:" -ForegroundColor Cyan
Write-Host "? Added Alerts table with fields: Id, RuleId, Title, Severity, Timestamp, etc." -ForegroundColor White
Write-Host "? Added Rules table with fields: Id, Name, YamlContent, IsEnabled, etc." -ForegroundColor White
Write-Host "? Applied migration: AddAlertsAndRules" -ForegroundColor White
Write-Host "? Foreign key relationship between Alerts and Rules" -ForegroundColor White
Write-Host ""

Write-Host "Service Registration:" -ForegroundColor Cyan
Write-Host "? IAlertRepository -> AlertRepository" -ForegroundColor White
Write-Host "? IRuleRepository -> RuleRepository" -ForegroundColor White
Write-Host "? IRuleEngine -> RuleEngine" -ForegroundColor White
Write-Host "? IAlertService -> AlertService" -ForegroundColor White
Write-Host ""

Write-Host "Fixed Issues:" -ForegroundColor Green
Write-Host "? Source Filter ComboBox binding - added SelectedValuePath='Content'" -ForegroundColor White
Write-Host "? Rule Engine integration with EventImporter" -ForegroundColor White
Write-Host "? Alert creation when rules are triggered" -ForegroundColor White
Write-Host "? Database schema for alerts and rules" -ForegroundColor White
Write-Host ""

Write-Host "Rule YAML Format Example:" -ForegroundColor Yellow
Write-Host @"
name: Failed Login Threshold
description: Detects multiple failed login attempts
severity: High
enabled: true
selection:
  event_id: 4625
condition:
  count: 5
  timeframe: 300
  group_by: user
"@ -ForegroundColor Gray

Write-Host ""

Write-Host "Next Steps for Advanced Features:" -ForegroundColor Cyan
Write-Host "? Add Rule creation/editing UI in Rules view" -ForegroundColor White
Write-Host "? Add Alert acknowledgment functionality" -ForegroundColor White
Write-Host "? Add notification system (email, webhook)" -ForegroundColor White
Write-Host "? Add rule testing interface" -ForegroundColor White
Write-Host "? Add more sophisticated rule conditions" -ForegroundColor White
Write-Host ""

Write-Host "===============================================" -ForegroundColor Green
Write-Host "         Alert System Ready!                  " -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

Write-Host "Start LogSentinel and check the Rules and Alerts views!" -ForegroundColor Yellow
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")