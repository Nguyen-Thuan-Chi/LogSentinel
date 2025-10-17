using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Log_Sentinel.Helpers;
using LogSentinel.BUS.Interfaces;
using LogSentinel.BUS.Models;
using LogSentinel.DAL.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Log_Sentinel.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly IEventRepository _eventRepository;
        private readonly IAlertRepository _alertRepository;
        private readonly IRuleRepository _ruleRepository;
        private readonly IAlertService _alertService;

        private int _activeEventsCount;
        private int _activeRulesCount;
        private int _alertsCount;

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

        public ObservableCollection<EventSummary> RecentEvents { get; } = new();
        public ObservableCollection<RuleSummary> ActiveRules { get; } = new();
        public ObservableCollection<AlertDto> RecentAlerts { get; } = new();

        public ICommand RefreshCommand { get; }

        public DashboardViewModel(
            IEventRepository eventRepository,
            IAlertRepository alertRepository,
            IRuleRepository ruleRepository,
            IAlertService alertService)
        {
            _eventRepository = eventRepository;
            _alertRepository = alertRepository;
            _ruleRepository = ruleRepository;
            _alertService = alertService;

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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
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
