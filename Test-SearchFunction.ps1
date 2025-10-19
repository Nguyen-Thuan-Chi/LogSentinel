# Test Search Functionality in LogSentinel Events View
# This script will help verify that the search function works properly

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "    LogSentinel - Search Function Test        " -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Search Function Fixes Applied:" -ForegroundColor Green
Write-Host "? Added SearchText property to EventsViewModel" -ForegroundColor White
Write-Host "? Added FilteredSystemLogs collection for filtered results" -ForegroundColor White
Write-Host "? Added ApplyFilters() method with text search logic" -ForegroundColor White
Write-Host "? Updated UI to bind SearchText with real-time filtering" -ForegroundColor White
Write-Host "? Added Source filter ComboBox for additional filtering" -ForegroundColor White
Write-Host "? Connected DataGrid to FilteredSystemLogs collection" -ForegroundColor White
Write-Host ""

Write-Host "How Search Works Now:" -ForegroundColor Yellow
Write-Host "? Type in search box Å® SearchText property updates" -ForegroundColor White
Write-Host "? ApplyFilters() method is called automatically" -ForegroundColor White
Write-Host "? Searches in: Message, Level, Host, User, Process, Source" -ForegroundColor White
Write-Host "? Updates FilteredSystemLogs collection" -ForegroundColor White
Write-Host "? DataGrid shows filtered results in real-time" -ForegroundColor White
Write-Host ""

Write-Host "Testing Instructions:" -ForegroundColor Yellow
Write-Host "1. Start LogSentinel (Debug mode is fine)" -ForegroundColor White
Write-Host "2. Navigate to Events view (Log Viewer)" -ForegroundColor White
Write-Host "3. Wait for events to load" -ForegroundColor White
Write-Host "4. Try these search tests:" -ForegroundColor White
Write-Host "   ? Type 'Process' Å® should show Process Create events" -ForegroundColor Gray
Write-Host "   ? Type 'DNS' Å® should show DNS Query events" -ForegroundColor Gray
Write-Host "   ? Type 'Info' Å® should show Info level events" -ForegroundColor Gray
Write-Host "   ? Type 'Sysmon' Å® should show Sysmon events" -ForegroundColor Gray
Write-Host "   ? Clear search Å® should show all events again" -ForegroundColor Gray
Write-Host "5. Try Source filter dropdown:" -ForegroundColor White
Write-Host "   ? Select 'Sysmon' Å® shows only Sysmon events" -ForegroundColor Gray
Write-Host "   ? Select 'WindowsEventLog' Å® shows only Windows events" -ForegroundColor Gray
Write-Host "   ? Select 'All' Å® shows all events" -ForegroundColor Gray
Write-Host ""

Write-Host "Expected Behavior:" -ForegroundColor Green
Write-Host "? Search is case-insensitive" -ForegroundColor White
Write-Host "? Results update in real-time as you type" -ForegroundColor White
Write-Host "? Can combine search text with source filter" -ForegroundColor White
Write-Host "? Event counts update based on filtered results" -ForegroundColor White
Write-Host "? No lag or freezing when typing" -ForegroundColor White
Write-Host ""

Write-Host "Troubleshooting:" -ForegroundColor Yellow
Write-Host "? If search doesn't work: Check DataContext is properly set" -ForegroundColor White
Write-Host "? If events don't load: Run as Administrator" -ForegroundColor White
Write-Host "? If filtering is slow: Check event count (should be reasonable)" -ForegroundColor White
Write-Host ""

Write-Host "Technical Details:" -ForegroundColor Cyan
Write-Host "? SearchText property has UpdateSourceTrigger=PropertyChanged" -ForegroundColor Gray
Write-Host "? ApplyFilters() method uses LINQ Where() for filtering" -ForegroundColor Gray
Write-Host "? FilteredSystemLogs is bound to DataGrid ItemsSource" -ForegroundColor Gray
Write-Host "? Search works on multiple fields simultaneously" -ForegroundColor Gray
Write-Host "? UI thread is properly handled with Dispatcher.Invoke" -ForegroundColor Gray
Write-Host ""

Write-Host "Files Modified:" -ForegroundColor Cyan
Write-Host "? Log Sentinel\ViewModels\EventsViewModel.cs" -ForegroundColor Gray
Write-Host "? Log Sentinel\UI\EventsView.xaml" -ForegroundColor Gray
Write-Host "? Log Sentinel\UI\EventsView.xaml.cs" -ForegroundColor Gray
Write-Host ""

Write-Host "===============================================" -ForegroundColor Green
Write-Host "           Search Function Ready!             " -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

Write-Host "Start LogSentinel and test the search functionality!" -ForegroundColor Yellow
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")