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
    public class LogEntry : INotifyPropertyChanged
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
        private ObservableCollection<LogEntry> _systemLogs = new();
        private ObservableCollection<LogEntry> _eventReports = new();
        private int _totalToday;
        private int _warningCount;
        private int _errorCount;
        private int _criticalCount;
        private string _sourceFilter = "All"; // All, Sample, WindowsEventLog, Sysmon
        private bool _disposed = false;

        public ObservableCollection<LogEntry> SystemLogs
        {
            get => _systemLogs;
            set { _systemLogs = value; OnPropertyChanged(nameof(SystemLogs)); }
        }

        public ObservableCollection<LogEntry> EventReports
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
                _ = LoadDataAsync();
            }
        }

        public EventsViewModel()
        {
            // Default constructor for design-time
            _refreshTimer = new System.Timers.Timer(5000); // Refresh every 5 seconds
            _refreshTimer.Elapsed += async (sender, e) => await RefreshDataAsync();
            HookCollectionChanged();
        }

        public EventsViewModel(IServiceProvider serviceProvider) : this()
        {
            _serviceProvider = serviceProvider;
            _refreshTimer.Start(); // Start auto-refresh for real data
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                if (_serviceProvider == null) return;

                using var scope = _serviceProvider.CreateScope();
                var eventRepository = scope.ServiceProvider.GetService<IEventRepository>();
                var alertRepository = scope.ServiceProvider.GetService<IAlertRepository>();

                if (eventRepository != null)
                {
                    // Load today's events
                    var today = DateTime.Today;
                    var tomorrow = today.AddDays(1);
                    var allEvents = await eventRepository.GetByDateRangeAsync(today, tomorrow);
                    
                    // Apply source filter
                    var events = SourceFilter == "All" 
                        ? allEvents 
                        : allEvents.Where(e => e.Source == SourceFilter);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        SystemLogs.Clear();
                        foreach (var evt in events.Take(100))
                        {
                            var detailsPreview = string.IsNullOrEmpty(evt.DetailsJson) 
                                ? "" 
                                : evt.DetailsJson.Substring(0, Math.Min(50, evt.DetailsJson.Length));
                            var action = evt.Action ?? "";
                            
                            SystemLogs.Add(new LogEntry
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

                        // Update counts
                        TotalToday = events.Count();
                    });
                }

                if (alertRepository != null)
                {
                    // Load recent alerts
                    var alerts = await alertRepository.GetRecentAsync(60); // Last hour

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        EventReports.Clear();
                        foreach (var alert in alerts.Take(50))
                        {
                            EventReports.Add(new LogEntry
                            {
                                Id = alert.Id,
                                Time = alert.Timestamp.ToString("HH:mm:ss"),
                                Level = alert.Severity,
                                Message = alert.Title,
                                Host = alert.RuleName,
                                User = alert.IsAcknowledged ? "Acknowledged" : "New"
                            });
                        }
                    });
                }

                RecalculateCounters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
                
                // Show message that no real data is available yet
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SystemLogs.Clear();
                    SystemLogs.Add(new LogEntry
                    {
                        Time = DateTime.Now.ToString("HH:mm:ss"),
                        Level = "Info", 
                        Message = "Waiting for real-time events from Windows Event Log and Sysmon...",
                        Host = "LogSentinel",
                        User = "System",
                        Process = "LogSentinel.exe",
                        Source = "System"
                    });
                });
                
                RecalculateCounters();
            }
        }

        private void HookCollectionChanged()
        {
            SystemLogs.CollectionChanged += (_, __) => RecalculateCounters();
        }

        private void RecalculateCounters()
        {
            TotalToday = SystemLogs.Count;
            WarningCount = SystemLogs.Count(l => string.Equals(l.Level, "Warning", StringComparison.OrdinalIgnoreCase));
            ErrorCount = SystemLogs.Count(l => string.Equals(l.Level, "Error", StringComparison.OrdinalIgnoreCase));
            CriticalCount = SystemLogs.Count(l => string.Equals(l.Level, "Critical", StringComparison.OrdinalIgnoreCase));
        }

        public async Task RefreshDataAsync()
        {
            await LoadDataAsync();
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
                    _refreshTimer?.Stop();
                    _refreshTimer?.Dispose();
                }
                _disposed = true;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
