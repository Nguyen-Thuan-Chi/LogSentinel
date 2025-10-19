using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Timers;
using Log_Sentinel.Helpers;
using LogSentinel.BUS.Interfaces;
using LogSentinel.BUS.Models;
using LogSentinel.DAL.Repositories;
using Microsoft.Extensions.DependencyInjection;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace Log_Sentinel.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IEventRepository _eventRepository;
        private readonly IAlertRepository _alertRepository;
        private readonly IRuleRepository _ruleRepository;
        private readonly IAlertService _alertService;
        private readonly SettingsViewModel _settingsViewModel;
        private readonly System.Timers.Timer _refreshTimer;

        private int _activeEventsCount;
        private int _activeRulesCount;
        private int _alertsCount;
        private bool _disposed = false;

        public int ActiveEventsCount
        {
            get => _activeEventsCount;
            set { _activeEventsCount = value; OnPropertyChanged(); }
        }

        public int ActiveRulesCount
        {
            get => _activeRulesCount;
            set { _activeRulesCount = value; OnPropertyChanged(); }
        }

        public int AlertsCount
        {
            get => _alertsCount;
            set { _alertsCount = value; OnPropertyChanged(); }
        }

        public bool IsProfessionalMode
        {
            get => _settingsViewModel?.DisplayMode == "Professional";
        }

        public PlotModel AlertsTimelineChart { get; private set; } = new();
        public PlotModel TopRulesChart { get; private set; } = new();

        public ObservableCollection<EventSummary> RecentEvents { get; } = new();
        public ObservableCollection<RuleSummary> ActiveRules { get; } = new();
        public ObservableCollection<AlertDto> RecentAlerts { get; } = new();

        public ICommand RefreshCommand { get; }

        public DashboardViewModel(
            IEventRepository eventRepository,
            IAlertRepository alertRepository,
            IRuleRepository ruleRepository,
            IAlertService alertService,
            SettingsViewModel settingsViewModel)
        {
            _eventRepository = eventRepository;
            _alertRepository = alertRepository;
            _ruleRepository = ruleRepository;
            _alertService = alertService;
            _settingsViewModel = settingsViewModel;

            // Listen to DisplayMode changes
            _settingsViewModel.PropertyChanged += OnSettingsChanged;

            // Setup auto-refresh timer (every 10 seconds for dashboard)
            _refreshTimer = new System.Timers.Timer(10000);
            _refreshTimer.Elapsed += async (sender, e) => await LoadDataAsync();
            _refreshTimer.Start();

            RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());

            // Subscribe to alert events
            _alertService.AlertCreated += OnAlertCreated;

            // Initial load - use Dispatcher to avoid blocking constructor
            _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => await LoadDataAsync());
        }

        private void OnAlertCreated(object? sender, AlertDto alert)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                RecentAlerts.Insert(0, alert);
                if (RecentAlerts.Count > 10)
                    RecentAlerts.RemoveAt(RecentAlerts.Count - 1);

                AlertsCount++;
            });
        }

        private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsViewModel.DisplayMode))
            {
                OnPropertyChanged(nameof(IsProfessionalMode));
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Load stats
                var events = await _eventRepository.GetAllAsync();
                var alerts = await _alertRepository.GetAllAsync();
                var rules = await _ruleRepository.GetEnabledAsync();

                ActiveEventsCount = events.Count();
                ActiveRulesCount = rules.Count();
                AlertsCount = alerts.Count(a => !a.IsAcknowledged);

                // Load recent events
                var recentEvents = events
                    .OrderByDescending(e => e.EventTime)
                    .Take(10)
                    .Select(e => new EventSummary
                    {
                        EventId = e.EventId,
                        Description = $"{e.Provider ?? "Unknown"} - {e.Action ?? "N/A"}",
                        Date = e.EventTime.ToString("yyyy-MM-dd HH:mm"),
                        Status = e.Level ?? "Info"
                    });

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    RecentEvents.Clear();
                    foreach (var evt in recentEvents)
                        RecentEvents.Add(evt);
                });

                // Load active rules
                var activeRules = rules
                    .Take(10)
                    .Select(r => new RuleSummary
                    {
                        RuleId = r.Id,
                        Name = r.Name ?? "Unnamed Rule",
                        Severity = r.Severity ?? "Medium",
                        Status = r.IsEnabled ? "Active" : "Disabled"
                    });

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ActiveRules.Clear();
                    foreach (var rule in activeRules)
                        ActiveRules.Add(rule);
                });

                // Load recent alerts
                var recentAlerts = await _alertService.GetRecentAlertsAsync(60);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    RecentAlerts.Clear();
                    foreach (var alert in recentAlerts.Take(10))
                        RecentAlerts.Add(alert);
                });

                // Load chart data
                await LoadChartsDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
            }
        }

        private async Task LoadChartsDataAsync()
        {
            try
            {
                // Load alerts timeline data (last 24 hours)
                var now = DateTime.Now;
                var last24Hours = now.AddHours(-24);
                
                var alerts = await _alertRepository.GetAllAsync();
                var timelineData = alerts
                    .Where(a => a.Timestamp >= last24Hours)
                    .GroupBy(a => a.Timestamp.Hour)
                    .Select(g => new { Hour = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Hour)
                    .ToList();

                // Create timeline chart
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    AlertsTimelineChart = new PlotModel { Title = "Alerts Timeline (Last 24 Hours)" };
                    
                    var categoryAxis = new CategoryAxis 
                    { 
                        Position = AxisPosition.Bottom,
                        Title = "Hour"
                    };
                    
                    var valueAxis = new LinearAxis 
                    { 
                        Position = AxisPosition.Left,
                        Title = "Alerts Count",
                        Minimum = 0
                    };
                    
                    AlertsTimelineChart.Axes.Add(categoryAxis);
                    AlertsTimelineChart.Axes.Add(valueAxis);

                    var lineSeries = new LineSeries
                    {
                        Title = "Alerts",
                        Color = OxyColors.SteelBlue,
                        StrokeThickness = 2,
                        MarkerType = MarkerType.Circle,
                        MarkerSize = 4,
                        MarkerFill = OxyColors.SteelBlue
                    };

                    for (int i = 0; i < 24; i++)
                    {
                        var hour = (now.Hour - 23 + i) % 24;
                        if (hour < 0) hour += 24;
                        
                        var count = timelineData.FirstOrDefault(x => x.Hour == hour)?.Count ?? 0;
                        lineSeries.Points.Add(new DataPoint(i, count));
                        categoryAxis.Labels.Add($"{hour:00}:00");
                    }

                    AlertsTimelineChart.Series.Add(lineSeries);
                    OnPropertyChanged(nameof(AlertsTimelineChart));
                });

                // Load top 5 rules data
                var topRules = alerts
                    .GroupBy(a => a.RuleName)
                    .Select(g => new { RuleName = g.Key ?? "Unknown Rule", Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToList();

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    TopRulesChart = new PlotModel 
                    { 
                        Title = "Top 5 Most Triggered Rules",
                        Background = OxyColors.White,
                        PlotAreaBorderThickness = new OxyThickness(1, 0, 0, 1),
                        PlotAreaBorderColor = OxyColors.LightGray
                    };

                    // Create horizontal bar chart
                    var categoryAxis = new CategoryAxis
                    {
                        Position = AxisPosition.Left,
                        Title = "Rules",
                        TitleFontSize = 14,
                        FontSize = 12,
                        TickStyle = TickStyle.None,
                        AxislineStyle = LineStyle.Solid,
                        AxislineColor = OxyColors.LightGray
                    };

                    var valueAxis = new LinearAxis
                    {
                        Position = AxisPosition.Bottom,
                        Title = "Trigger Count",
                        TitleFontSize = 14,
                        FontSize = 12,
                        Minimum = 0,
                        MajorGridlineStyle = LineStyle.Solid,
                        MajorGridlineColor = OxyColors.LightGray,
                        MinorGridlineStyle = LineStyle.Dot,
                        MinorGridlineColor = OxyColors.LightGray,
                        AxislineStyle = LineStyle.Solid,
                        AxislineColor = OxyColors.LightGray
                    };

                    TopRulesChart.Axes.Add(categoryAxis);
                    TopRulesChart.Axes.Add(valueAxis);

                    var barSeries = new BarSeries
                    {
                        Title = "Trigger Count",
                        LabelPlacement = LabelPlacement.Outside,
                        LabelFormatString = "{0}",
                        FillColor = OxyColor.FromRgb(37, 99, 235), // #2563EB - consistent with your blue theme
                        StrokeColor = OxyColor.FromRgb(29, 78, 216), // Slightly darker blue for stroke
                        StrokeThickness = 1
                    };

                    // Add data to chart (reverse order for proper display in horizontal bar chart)
                    var reversedRules = topRules.AsEnumerable().Reverse().ToList();
                    for (int i = 0; i < reversedRules.Count; i++)
                    {
                        var rule = reversedRules[i];
                        barSeries.Items.Add(new BarItem(rule.Count, i));
                        categoryAxis.Labels.Add(rule.RuleName);
                    }

                    // Set a reasonable maximum for the value axis based on data
                    if (topRules.Any())
                    {
                        var maxValue = topRules.Max(r => r.Count);
                        var roundedMax = (int)(Math.Ceiling(maxValue / 25.0) * 25); // Round to nearest 25
                        valueAxis.Maximum = Math.Max(roundedMax, 25); // Ensure minimum scale
                    }

                    TopRulesChart.Series.Add(barSeries);
                    OnPropertyChanged(nameof(TopRulesChart));
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading charts data: {ex.Message}");
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
                    _refreshTimer?.Stop();
                    _refreshTimer?.Dispose();
                    if (_alertService != null)
                        _alertService.AlertCreated -= OnAlertCreated;
                    if (_settingsViewModel != null)
                        _settingsViewModel.PropertyChanged -= OnSettingsChanged;
                }
                _disposed = true;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class EventSummary
    {
        public int? EventId { get; set; }
        public string Description { get; set; } = "";
        public string Date { get; set; } = "";
        public string Status { get; set; } = "";
    }

    public class RuleSummary
    {
        public long RuleId { get; set; }
        public string Name { get; set; } = "";
        public string Severity { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
