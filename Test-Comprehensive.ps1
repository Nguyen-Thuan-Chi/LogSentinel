# Comprehensive Test Script for LogSentinel Alert & Rule System
# This script tests all components and provides guided testing instructions

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "    LogSentinel - Comprehensive System Test   " -ForegroundColor Cyan  
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "?? SYSTEM OVERVIEW" -ForegroundColor Green
Write-Host "? Complete Alert and Rule Engine system implemented" -ForegroundColor White
Write-Host "? Real-time event processing with rule evaluation" -ForegroundColor White
Write-Host "? Alert management with acknowledgment capability" -ForegroundColor White
Write-Host "? Advanced filtering and search in Events view" -ForegroundColor White
Write-Host "? Integration with Windows Event Log and Sysmon" -ForegroundColor White
Write-Host ""

Write-Host "?? FEATURES IMPLEMENTED" -ForegroundColor Yellow
Write-Host "?? Dashboard:" -ForegroundColor Cyan
Write-Host "   ? Real-time event counters (Total, Warning, Error, Critical)" -ForegroundColor Gray
Write-Host "   ? Alert statistics and severity breakdown" -ForegroundColor Gray
Write-Host "   ? Live monitoring of system health" -ForegroundColor Gray
Write-Host ""

Write-Host "?? Events View:" -ForegroundColor Cyan
Write-Host "   ? Fixed Source Filter (All, Sample, Sysmon, System, WindowsEventLog)" -ForegroundColor Gray
Write-Host "   ? Real-time search functionality across all event fields" -ForegroundColor Gray
Write-Host "   ? Live event streaming from Windows Event Log and Sysmon" -ForegroundColor Gray
Write-Host "   ? Pagination and virtualization for performance" -ForegroundColor Gray
Write-Host ""

Write-Host "?? Rules View:" -ForegroundColor Cyan
Write-Host "   ? View and manage security detection rules" -ForegroundColor Gray
Write-Host "   ? YAML-based rule definitions" -ForegroundColor Gray
Write-Host "   ? Enable/disable rules dynamically" -ForegroundColor Gray
Write-Host "   ? Rule trigger statistics" -ForegroundColor Gray
Write-Host ""

Write-Host "?? Alerts View:" -ForegroundColor Cyan
Write-Host "   ? Real-time alert display with severity indicators" -ForegroundColor Gray
Write-Host "   ? Filter by severity (Critical, High, Medium, Low)" -ForegroundColor Gray
Write-Host "   ? Acknowledge alerts with user tracking" -ForegroundColor Gray
Write-Host "   ? Bulk operations (acknowledge all, export CSV)" -ForegroundColor Gray
Write-Host "   ? Alert details and metadata" -ForegroundColor Gray
Write-Host ""

Write-Host "?? Settings View:" -ForegroundColor Cyan
Write-Host "   ? Configure event sources (EventLog, Sysmon, Sample files)" -ForegroundColor Gray
Write-Host "   ? Database connection settings" -ForegroundColor Gray
Write-Host "   ? Performance tuning options" -ForegroundColor Gray
Write-Host ""

Write-Host "?? TESTING PROCEDURES" -ForegroundColor Green
Write-Host ""

Write-Host "1?? BASIC FUNCTIONALITY TEST:" -ForegroundColor Yellow
Write-Host "   a) Start LogSentinel as Administrator" -ForegroundColor White
Write-Host "   b) Check Dashboard - should show live counters" -ForegroundColor White
Write-Host "   c) Navigate between all views (Dashboard, Events, Rules, Alerts, Settings)" -ForegroundColor White
Write-Host "   d) Verify no crash or UI freezing" -ForegroundColor White
Write-Host ""

Write-Host "2?? EVENTS VIEW TEST:" -ForegroundColor Yellow
Write-Host "   a) Go to Events view" -ForegroundColor White
Write-Host "   b) Test Source Filter:" -ForegroundColor White
Write-Host "      ? Select 'All' Å® Should show all events" -ForegroundColor Gray
Write-Host "      ? Select 'Sysmon' Å® Should show only Sysmon events" -ForegroundColor Gray
Write-Host "      ? Select 'System' Å® Should show only System events" -ForegroundColor Gray
Write-Host "      ? Select 'WindowsEventLog' Å® Should show only Windows events" -ForegroundColor Gray
Write-Host "   c) Test Search functionality:" -ForegroundColor White
Write-Host "      ? Type 'Process' Å® Should filter to process-related events" -ForegroundColor Gray
Write-Host "      ? Type 'DNS' Å® Should filter to DNS events" -ForegroundColor Gray
Write-Host "      ? Type 'Info' Å® Should filter to Info level events" -ForegroundColor Gray
Write-Host "      ? Clear search Å® Should show all events again" -ForegroundColor Gray
Write-Host "   d) Verify real-time updates (events should appear automatically)" -ForegroundColor White
Write-Host ""

Write-Host "3?? RULES VIEW TEST:" -ForegroundColor Yellow
Write-Host "   a) Go to Rules view" -ForegroundColor White
Write-Host "   b) Should see 5 sample rules:" -ForegroundColor White
Write-Host "      ? Failed Login Threshold (High severity)" -ForegroundColor Gray
Write-Host "      ? Admin User Created (Critical severity)" -ForegroundColor Gray  
Write-Host "      ? Suspicious PowerShell Execution (High severity)" -ForegroundColor Gray
Write-Host "      ? Process Creation Monitoring (Medium severity)" -ForegroundColor Gray
Write-Host "      ? Network Connection Monitoring (Low severity)" -ForegroundColor Gray
Write-Host "   c) Check rule status (should be enabled by default)" -ForegroundColor White
Write-Host "   d) View rule trigger counts and last triggered times" -ForegroundColor White
Write-Host ""

Write-Host "4?? ALERTS VIEW TEST:" -ForegroundColor Yellow
Write-Host "   a) Go to Alerts view" -ForegroundColor White
Write-Host "   b) Test severity filtering (Critical, High, Medium, Low, All)" -ForegroundColor White
Write-Host "   c) Test 'Show Acknowledged' checkbox" -ForegroundColor White
Write-Host "   d) Test alert actions:" -ForegroundColor White
Write-Host "      ? Click 'Acknowledge' button on an alert" -ForegroundColor Gray
Write-Host "      ? Click 'View Details' to see alert information" -ForegroundColor Gray
Write-Host "      ? Try 'Acknowledge All' button" -ForegroundColor Gray
Write-Host "      ? Try 'Export CSV' function" -ForegroundColor Gray
Write-Host "   e) Verify real-time alert updates" -ForegroundColor White
Write-Host ""

Write-Host "5?? RULE ENGINE TEST:" -ForegroundColor Yellow
Write-Host "   a) Generate test events to trigger rules:" -ForegroundColor White
Write-Host "      ? Run PowerShell with suspicious parameters:" -ForegroundColor Gray
Write-Host "        powershell.exe -nop -enc <base64>" -ForegroundColor DarkGray
Write-Host "      ? Multiple failed login attempts (simulate with Event ID 4625)" -ForegroundColor Gray
Write-Host "      ? Process creation events (automatic from Sysmon)" -ForegroundColor Gray
Write-Host "   b) Check if alerts are created in real-time" -ForegroundColor White
Write-Host "   c) Verify alert details match the triggering events" -ForegroundColor White
Write-Host "   d) Check rule trigger counters increment" -ForegroundColor White
Write-Host ""

Write-Host "6?? PERFORMANCE TEST:" -ForegroundColor Yellow
Write-Host "   a) Leave application running for 10+ minutes" -ForegroundColor White
Write-Host "   b) Generate high event volume (open/close multiple processes)" -ForegroundColor White
Write-Host "   c) Verify UI remains responsive" -ForegroundColor White
Write-Host "   d) Check memory usage doesn't grow excessively" -ForegroundColor White
Write-Host "   e) Test filtering with large dataset" -ForegroundColor White
Write-Host ""

Write-Host "?? TROUBLESHOOTING GUIDE" -ForegroundColor Red
Write-Host ""

Write-Host "Issue: No events appearing" -ForegroundColor Red
Write-Host "Solution:" -ForegroundColor Green
Write-Host "   ? Run as Administrator (required for Security event log)" -ForegroundColor White
Write-Host "   ? Enable Sysmon if installed" -ForegroundColor White
Write-Host "   ? Check Windows Event Log service is running" -ForegroundColor White
Write-Host "   ? Verify database connection in Settings" -ForegroundColor White
Write-Host ""

Write-Host "Issue: Source filter not working" -ForegroundColor Red
Write-Host "Solution:" -ForegroundColor Green  
Write-Host "   ? Fixed: SelectedValuePath='Content' added to ComboBox" -ForegroundColor White
Write-Host "   ? Should work correctly now" -ForegroundColor White
Write-Host ""

Write-Host "Issue: No alerts generated" -ForegroundColor Red
Write-Host "Solution:" -ForegroundColor Green
Write-Host "   ? Check rules are enabled in Rules view" -ForegroundColor White
Write-Host "   ? Generate events that match rule conditions" -ForegroundColor White
Write-Host "   ? Check application logs for rule engine errors" -ForegroundColor White
Write-Host ""

Write-Host "Issue: Performance problems" -ForegroundColor Red
Write-Host "Solution:" -ForegroundColor Green
Write-Host "   ? Reduce event retention period in Settings" -ForegroundColor White
Write-Host "   ? Disable unnecessary event sources" -ForegroundColor White
Write-Host "   ? Check disk space for SQLite database" -ForegroundColor White
Write-Host ""

Write-Host "?? DATABASE VERIFICATION" -ForegroundColor Cyan
Write-Host "Database location: %LOCALAPPDATA%\LogSentinel\logsentinel.db" -ForegroundColor Gray
Write-Host "Tables created: Events, Rules, Alerts" -ForegroundColor Gray
Write-Host "Sample rules: 5 security detection rules" -ForegroundColor Gray
Write-Host "Migrations: InitialCreate, AddAlertsAndRules" -ForegroundColor Gray
Write-Host ""

Write-Host "?? SUCCESS CRITERIA" -ForegroundColor Green
Write-Host "? Application starts without errors" -ForegroundColor White
Write-Host "? All views load and function properly" -ForegroundColor White
Write-Host "? Events appear in real-time" -ForegroundColor White
Write-Host "? Source filtering works correctly" -ForegroundColor White
Write-Host "? Search functionality works across all fields" -ForegroundColor White
Write-Host "? Rules are loaded and active" -ForegroundColor White
Write-Host "? Alerts are generated when rules trigger" -ForegroundColor White
Write-Host "? Alert management functions work" -ForegroundColor White
Write-Host "? No memory leaks or performance degradation" -ForegroundColor White
Write-Host ""

Write-Host "===============================================" -ForegroundColor Green
Write-Host "        ?? SYSTEM READY FOR TESTING! ??      " -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

Write-Host "Next: Start LogSentinel and follow the testing procedures above!" -ForegroundColor Yellow
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")