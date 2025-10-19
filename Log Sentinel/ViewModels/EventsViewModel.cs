using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Timers;
using LogSentinel.DAL.Data;
using LogSentinel.DAL.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Log_Sentinel.ViewModels
{
    // Simple log entry model for UI display
    public class LogEntryUI : INotifyPropertyChanged
    {
        private long _id;
        private string _time = string.Empty;
        private string _level = string.Empty;
        private string _message = string.Empty;
        private string _host = string.Empty;
        private string _user = string.Empty;
        private string _process = string.Empty;
        private string _source = string.Empty;

        public long Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }

        public string Time
        {
            get => _time;
            set { _time = value; OnPropertyChanged(nameof(Time)); }
        }

        public string Level
        {
            get => _level;
            set { _level = value; OnPropertyChanged(nameof(Level)); }
        }

        public string Message
        {
            get => _message;
            set { _message = value; OnPropertyChanged(nameof(Message)); }
        }

        public string Host
        {
            get => _host;
            set { _host = value; OnPropertyChanged(nameof(Host)); }
        }

        public string User
        {
            get => _user;
            set { _user = value; OnPropertyChanged(nameof(User)); }
        }

        public string Process
        {
            get => _process;
            set { _process = value; OnPropertyChanged(nameof(Process)); }
        }

        public string Source
        {
            get => _source;
            set { _source = value; OnPropertyChanged(nameof(Source)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Optimized EventsViewModel with real data integration
    public class EventsViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IServiceProvider? _serviceProvider;
        private readonly System.Timers.Timer _refreshTimer;
        private ObservableCollection<LogEntryUI> _systemLogs = new();
        private ObservableCollection<LogEntryUI> _filteredSystemLogs = new();
        private ObservableCollection<LogEntryUI> _eventReports = new();
        private int _totalToday;
        private int _warningCount;
        private int _errorCount;
        private int _criticalCount;
        private string _sourceFilter = "All"; // All, Sample, WindowsEventLog, Sysmon
        private string _searchText = string.Empty;
        private string _displayMode = "Professional"; // User, Professional
        private bool _disposed = false;

        public ObservableCollection<LogEntryUI> SystemLogs
        {
            get => _systemLogs;
            set { _systemLogs = value; OnPropertyChanged(nameof(SystemLogs)); }
        }

        public ObservableCollection<LogEntryUI> FilteredSystemLogs
        {
            get => _filteredSystemLogs;
            set { _filteredSystemLogs = value; OnPropertyChanged(nameof(FilteredSystemLogs)); }
        }

        public ObservableCollection<LogEntryUI> EventReports
        {
            get => _eventReports;
            set { _eventReports = value; OnPropertyChanged(nameof(EventReports)); }
        }

        public int TotalToday
        {
            get => _totalToday;
            private set { _totalToday = value; OnPropertyChanged(nameof(TotalToday)); }
        }

        public int WarningCount
        {
            get => _warningCount;
            private set { _warningCount = value; OnPropertyChanged(nameof(WarningCount)); }
        }

        public int ErrorCount
        {
            get => _errorCount;
            private set { _errorCount = value; OnPropertyChanged(nameof(ErrorCount)); }
        }

        public int CriticalCount
        {
            get => _criticalCount;
            private set { _criticalCount = value; OnPropertyChanged(nameof(CriticalCount)); }
        }

        public string SourceFilter
        {
            get => _sourceFilter;
            set
            {
                _sourceFilter = value;
                OnPropertyChanged(nameof(SourceFilter));
                ApplyFilters();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                ApplyFilters();
            }
        }

        public string DisplayMode
        {
            get => _displayMode;
            set
            {
                _displayMode = value;
                OnPropertyChanged(nameof(DisplayMode));
                // Không cần ApplyFilters vì DisplayMode chỉ ảnh hưởng đến UI display
            }
        }

        public EventsViewModel()
        {
            // Default constructor for design-time
            _refreshTimer = new System.Timers.Timer(5000); // Refresh every 5 seconds
            _refreshTimer.Elapsed += async (sender, e) =>
            {
                try
                {
                    await RefreshDataAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in timer refresh: {ex.Message}");
                }
            };
            HookCollectionChanged();
        }

        public EventsViewModel(IServiceProvider serviceProvider) : this()
        {
            _serviceProvider = serviceProvider;
            try
            {
                _refreshTimer.Start(); // Start auto-refresh for real data
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await LoadDataAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in initial load: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting ViewModel: {ex.Message}");
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                if (_serviceProvider == null) 
                {
                    await LoadFallbackDataAsync();
                    return;
                }

                using var scope = _serviceProvider.CreateScope();
                var eventRepository = scope.ServiceProvider.GetService<IEventRepository>();
                var alertRepository = scope.ServiceProvider.GetService<IAlertRepository>();

                if (eventRepository != null)
                {
                    // Load today's events
                    var today = DateTime.Today;
                    var tomorrow = today.AddDays(1);
                    var events = await eventRepository.GetByDateRangeAsync(today, tomorrow);

                    if (Application.Current?.Dispatcher != null)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            try
                            {
                                SystemLogs.Clear();
                                foreach (var evt in events.Take(100))
                                {
                                    if (evt == null) continue;

                                    var detailsPreview = string.IsNullOrEmpty(evt.DetailsJson) 
                                        ? "" 
                                        : evt.DetailsJson.Substring(0, Math.Min(50, evt.DetailsJson.Length));
                                    var action = evt.Action ?? "";
                                    
                                    SystemLogs.Add(new LogEntryUI
                                    {
                                        Id = evt.Id,
                                        Time = evt.EventTime.ToString("HH:mm:ss"),
                                        Level = evt.Level ?? "Info",
                                        Message = action + (string.IsNullOrEmpty(detailsPreview) ? "" : " - " + detailsPreview),
                                        Host = evt.Host ?? "",
                                        User = evt.User ?? "",
                                        Process = evt.Process ?? "",
                                        Source = evt.Source ?? "Sample"
                                    });
                                }

                                // Apply filters after loading data
                                ApplyFilters();
                            }
                            catch (Exception uiEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error updating UI: {uiEx.Message}");
                            }
                        });
                    }
                }

                if (alertRepository != null)
                {
                    // Load ALL alerts (not just recent ones)
                    var alerts = await alertRepository.GetAllAsync(); 

                    if (Application.Current?.Dispatcher != null)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            try
                            {
                                EventReports.Clear();
                                foreach (var alert in alerts.OrderByDescending(a => a.Timestamp).Take(100))
                                {
                                    if (alert == null) continue;

                                    EventReports.Add(new LogEntryUI
                                    {
                                        Id = alert.Id,
                                        Time = alert.Timestamp.ToString("HH:mm:ss"),
                                        Level = alert.Severity ?? "Medium",
                                        Message = alert.Title ?? "Unknown Alert",
                                        Host = alert.RuleName ?? "Unknown Rule",
                                        User = alert.IsAcknowledged ? "Acknowledged" : "New"
                                    });
                                }
                            }
                            catch (Exception uiEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error updating alerts UI: {uiEx.Message}");
                            }
                        });
                    }
                }

                // Recalculate counters on UI thread
                if (Application.Current?.Dispatcher != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() => RecalculateCounters());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
                await LoadFallbackDataAsync();
            }
        }

        private async Task LoadFallbackDataAsync()
        {
            try
            {
                if (Application.Current?.Dispatcher != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        SystemLogs.Clear();
                        SystemLogs.Add(new LogEntryUI
                        {
                            Time = DateTime.Now.ToString("HH:mm:ss"),
                            Level = "Info", 
                            Message = "Waiting for real-time events from Windows Event Log and Sysmon...",
                            Host = "LogSentinel",
                            User = "System",
                            Process = "LogSentinel.exe",
                            Source = "System"
                        });
                        
                        // Apply filters after loading placeholder data
                        ApplyFilters();
                        RecalculateCounters();
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading fallback data: {ex.Message}");
            }
        }

        private void HookCollectionChanged()
        {
            try
            {
                SystemLogs.CollectionChanged += (sender, e) =>
                {
                    try
                    {
                        ApplyFilters();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in collection changed handler: {ex.Message}");
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error hooking collection changed: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            try
            {
                if (SystemLogs == null)
                {
                    if (Application.Current?.Dispatcher != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            FilteredSystemLogs = new ObservableCollection<LogEntryUI>();
                        });
                    }
                    return;
                }

                var filtered = SystemLogs.AsEnumerable();

                // Apply source filter (but only for display filtering, not data loading)
                if (!string.IsNullOrWhiteSpace(SourceFilter) && SourceFilter != "All")
                {
                    filtered = filtered.Where(log => 
                        string.Equals(log?.Source, SourceFilter, StringComparison.OrdinalIgnoreCase));
                }

                // Apply search text filter
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLowerInvariant();
                    filtered = filtered.Where(log => log != null && (
                        (log.Message?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                        (log.Level?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                        (log.Host?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                        (log.User?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                        (log.Process?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                        (log.Source?.ToLowerInvariant().Contains(searchLower) ?? false)));
                }

                // Update filtered collection on UI thread
                if (Application.Current?.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            FilteredSystemLogs.Clear();
                            foreach (var item in filtered.Where(x => x != null))
                            {
                                FilteredSystemLogs.Add(item);
                            }
                            RecalculateCounters();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error updating filtered collection: {ex.Message}");
                        }
                    });
                }
                else
                {
                    // Fallback for non-UI thread scenarios
                    RecalculateCounters();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying filters: {ex.Message}");
            }
        }

        private void RecalculateCounters()
        {
            try
            {
                var logsToCount = FilteredSystemLogs?.Count > 0 ? FilteredSystemLogs : SystemLogs;
                if (logsToCount == null)
                {
                    TotalToday = 0;
                    WarningCount = 0;
                    ErrorCount = 0;
                    CriticalCount = 0;
                    return;
                }

                TotalToday = logsToCount.Count;
                WarningCount = logsToCount.Count(l => l != null && string.Equals(l.Level, "Warning", StringComparison.OrdinalIgnoreCase));
                ErrorCount = logsToCount.Count(l => l != null && string.Equals(l.Level, "Error", StringComparison.OrdinalIgnoreCase));
                CriticalCount = logsToCount.Count(l => l != null && string.Equals(l.Level, "Critical", StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error recalculating counters: {ex.Message}");
                // Set safe defaults
                TotalToday = 0;
                WarningCount = 0;
                ErrorCount = 0;
                CriticalCount = 0;
            }
        }

        public async Task RefreshDataAsync()
        {
            try
            {
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing data: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        _refreshTimer?.Stop();
                        _refreshTimer?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error disposing timer: {ex.Message}");
                    }
                }
                _disposed = true;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
