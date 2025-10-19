using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using Log_Sentinel.Helpers;
using LogSentinel.BUS.Interfaces;
using LogSentinel.BUS.Models;
using LogSentinel.DAL.Repositories;
using Microsoft.Win32;

namespace Log_Sentinel.ViewModels
{
    public class AlertsViewModel : INotifyPropertyChanged
    {
        private readonly IAlertRepository _alertRepository;
        private readonly IAlertService _alertService;
        private string _selectedSeverity = "All";
        private bool _showAcknowledged = true; // Show acknowledged alerts by default to see all alerts
        private AlertItem? _selectedAlert;

        public string SelectedSeverity
        {
            get => _selectedSeverity;
            set
            {
                _selectedSeverity = value;
                OnPropertyChanged();
                FilterAlerts();
            }
        }

        public bool ShowAcknowledged
        {
            get => _showAcknowledged;
            set
            {
                _showAcknowledged = value;
                OnPropertyChanged();
                FilterAlerts();
            }
        }

        public AlertItem? SelectedAlert
        {
            get => _selectedAlert;
            set
            {
                _selectedAlert = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<AlertItem> Alerts { get; } = new();
        public ObservableCollection<AlertItem> FilteredAlerts { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand AcknowledgeAlertCommand { get; }
        public ICommand AcknowledgeAllCommand { get; }
        public ICommand ViewDetailsCommand { get; }
        public ICommand DeleteAlertCommand { get; }
        public ICommand ExportCsvCommand { get; }

        public AlertsViewModel(IAlertRepository alertRepository, IAlertService alertService)
        {
            _alertRepository = alertRepository;
            _alertService = alertService;

            RefreshCommand = new RelayCommand(async _ => await LoadAlertsAsync());
            AcknowledgeAlertCommand = new RelayCommand(async param => await AcknowledgeAlertAsync(param as AlertItem), param => param is AlertItem);
            AcknowledgeAllCommand = new RelayCommand(async _ => await AcknowledgeAllAlertsAsync());
            ViewDetailsCommand = new RelayCommand(param => ViewAlertDetails(param as AlertItem), param => param is AlertItem);
            DeleteAlertCommand = new RelayCommand(async param => await DeleteAlertAsync(param as AlertItem), param => param is AlertItem);
            ExportCsvCommand = new RelayCommand(async _ => await ExportToCsvAsync());

            // Subscribe to alert events
            _alertService.AlertCreated += OnAlertCreated;

            // Load alerts when the view model is created
            _ = Task.Run(async () => await LoadAlertsAsync());
            
            // Set up periodic refresh to catch any missed alerts
            var refreshTimer = new System.Timers.Timer(30000); // Every 30 seconds
            refreshTimer.Elapsed += async (sender, e) => await LoadAlertsAsync();
            refreshTimer.Start();
        }

        private void OnAlertCreated(object? sender, AlertDto alert)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var alertItem = new AlertItem
                {
                    Id = alert.Id,
                    RuleName = alert.RuleName,
                    Severity = alert.Severity,
                    Timestamp = alert.Timestamp,
                    Title = alert.Title,
                    Description = alert.Description,
                    IsAcknowledged = alert.IsAcknowledged
                };

                Alerts.Insert(0, alertItem);
                FilterAlerts();
            });
        }

        private async Task LoadAlertsAsync()
        {
            try
            {
                var alerts = await _alertRepository.GetAllAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Alerts.Clear();
                    foreach (var alert in alerts.OrderByDescending(a => a.Timestamp))
                    {
                        Alerts.Add(new AlertItem
                        {
                            Id = alert.Id,
                            RuleName = alert.RuleName,
                            Severity = alert.Severity,
                            Timestamp = alert.Timestamp,
                            Title = alert.Title,
                            Description = alert.Description,
                            IsAcknowledged = alert.IsAcknowledged,
                            AcknowledgedBy = alert.AcknowledgedBy,
                            AcknowledgedAt = alert.AcknowledgedAt
                        });
                    }

                    FilterAlerts();
                    
                    // Debug: Show count in message box for troubleshooting (remove after testing)
                    System.Diagnostics.Debug.WriteLine($"Loaded {alerts.Count()} alerts from database");
                    
                    // Temporary: Show alert count in UI for debugging
                    if (alerts.Count() == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("No alerts found in database");
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading alerts: {ex.Message}\n\nStack trace: {ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterAlerts()
        {
            FilteredAlerts.Clear();

            var filtered = Alerts.AsEnumerable();

            // Filter by severity
            if (SelectedSeverity != "All")
            {
                filtered = filtered.Where(a => a.Severity.Equals(SelectedSeverity, StringComparison.OrdinalIgnoreCase));
            }

            // Filter by acknowledgment status
            if (!ShowAcknowledged)
            {
                filtered = filtered.Where(a => !a.IsAcknowledged);
            }

            foreach (var alert in filtered)
            {
                FilteredAlerts.Add(alert);
            }
            
            // Notify UI that the filtered collection has changed
            OnPropertyChanged(nameof(FilteredAlerts));
            
            // Debug output
            System.Diagnostics.Debug.WriteLine($"FilterAlerts: Total alerts: {Alerts.Count}, Filtered alerts: {FilteredAlerts.Count}, Selected severity: {SelectedSeverity}, Show acknowledged: {ShowAcknowledged}");
        }

        private async Task AcknowledgeAlertAsync(AlertItem? alertItem)
        {
            if (alertItem == null || alertItem.IsAcknowledged) return;

            try
            {
                await _alertService.AcknowledgeAlertAsync(alertItem.Id, Environment.UserName);
                
                alertItem.IsAcknowledged = true;
                alertItem.AcknowledgedBy = Environment.UserName;
                alertItem.AcknowledgedAt = DateTime.UtcNow;
                
                OnPropertyChanged(nameof(FilteredAlerts));
                FilterAlerts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error acknowledging alert: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task AcknowledgeAllAlertsAsync()
        {
            var unacknowledged = Alerts.Where(a => !a.IsAcknowledged).ToList();

            if (!unacknowledged.Any())
            {
                MessageBox.Show("No unacknowledged alerts found.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to acknowledge all {unacknowledged.Count} unacknowledged alerts?",
                "Confirm Acknowledge All",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    foreach (var alert in unacknowledged)
                    {
                        await _alertService.AcknowledgeAlertAsync(alert.Id, Environment.UserName);
                        alert.IsAcknowledged = true;
                        alert.AcknowledgedBy = Environment.UserName;
                        alert.AcknowledgedAt = DateTime.UtcNow;
                    }

                    FilterAlerts();
                    MessageBox.Show($"Successfully acknowledged {unacknowledged.Count} alerts.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error acknowledging alerts: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewAlertDetails(AlertItem? alertItem)
        {
            if (alertItem == null) return;

            var details = $"Alert Details\n\n" +
                         $"ID: {alertItem.Id}\n" +
                         $"Rule: {alertItem.RuleName}\n" +
                         $"Severity: {alertItem.Severity}\n" +
                         $"Time: {alertItem.Timestamp:yyyy-MM-dd HH:mm:ss}\n" +
                         $"Title: {alertItem.Title}\n" +
                         $"Description: {alertItem.Description}\n" +
                         $"Status: {alertItem.StatusText}\n";

            if (alertItem.IsAcknowledged)
            {
                details += $"Acknowledged By: {alertItem.AcknowledgedBy}\n" +
                          $"Acknowledged At: {alertItem.AcknowledgedAt:yyyy-MM-dd HH:mm:ss}";
            }

            MessageBox.Show(details, "Alert Details", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task DeleteAlertAsync(AlertItem? alertItem)
        {
            if (alertItem == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete this alert?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var alert = await _alertRepository.GetByIdAsync(alertItem.Id);
                    if (alert != null)
                    {
                        await _alertRepository.DeleteAsync(alert);
                        Alerts.Remove(alertItem);
                        FilterAlerts();
                        MessageBox.Show("Alert deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting alert: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ExportToCsvAsync()
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    FileName = $"alerts_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    await _alertService.ExportToCsvAsync(dialog.FileName);
                    MessageBox.Show($"Alerts exported successfully to:\n{dialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting alerts: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class AlertItem : INotifyPropertyChanged
    {
        private bool _isAcknowledged;

        public long Id { get; set; }
        public string RuleName { get; set; } = "";
        public string Severity { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";

        public bool IsAcknowledged
        {
            get => _isAcknowledged;
            set
            {
                _isAcknowledged = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public string? AcknowledgedBy { get; set; }
        public DateTime? AcknowledgedAt { get; set; }

        public string StatusText => IsAcknowledged ? "Acknowledged" : "New";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
