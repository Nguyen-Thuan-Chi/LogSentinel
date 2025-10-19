using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Log_Sentinel.Helpers;
using Log_Sentinel.UI;
using LogSentinel.BUS.Interfaces;
using LogSentinel.BUS.Models;
using LogSentinel.DAL.Data;
using LogSentinel.DAL.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace Log_Sentinel.ViewModels
{
    public partial class AlertsViewModel : ObservableObject
    {
        private readonly IAlertRepository _alertRepository;
        private readonly IAlertService _alertService;
        private readonly IServiceProvider _serviceProvider;
        
        [ObservableProperty]
        private string selectedSeverity = "All";
        
        [ObservableProperty]
        private bool showAcknowledged = true; // Show acknowledged alerts by default to see all alerts
        
        [ObservableProperty]
        private AlertItem? selectedAlert;

        partial void OnSelectedSeverityChanged(string value)
        {
            FilterAlerts();
        }

        partial void OnShowAcknowledgedChanged(bool value)
        {
            FilterAlerts();
        }

        public ObservableCollection<AlertItem> Alerts { get; } = new();
        public ObservableCollection<AlertItem> FilteredAlerts { get; } = new();

        public AlertsViewModel(IAlertRepository alertRepository, IAlertService alertService, IServiceProvider serviceProvider)
        {
            _alertRepository = alertRepository;
            _alertService = alertService;
            _serviceProvider = serviceProvider;

            // Subscribe to alert events
            _alertService.AlertCreated += OnAlertCreated;

            // Load alerts when the view model is created
            _ = Task.Run(async () => await LoadAlertsAsync());
            
            // Set up periodic refresh to catch any missed alerts
            var refreshTimer = new System.Timers.Timer(30000); // Every 30 seconds
            refreshTimer.Elapsed += async (sender, e) => await LoadAlertsAsync();
            refreshTimer.Start();
        }

        [RelayCommand]
        private async Task LoadAlerts()
        {
            await LoadAlertsAsync();
        }

        [RelayCommand]
        private async Task AcknowledgeAlert(AlertItem? alertItem)
        {
            await AcknowledgeAlertAsync(alertItem);
        }

        [RelayCommand]
        private async Task AcknowledgeAll()
        {
            await AcknowledgeAllAlertsAsync();
        }

        [RelayCommand(CanExecute = nameof(CanShowDetail))]
        private async Task ShowDetail()
        {
            // Defensive null-check for SelectedAlert
            if (SelectedAlert == null) return;

            try
            {
                // Get the full AlertEntity from the database
                var alertEntity = await _alertRepository.GetByIdAsync(SelectedAlert.Id);
                if (alertEntity != null)
                {
                    // Create and show the detail window with safe exception handling
                    var detailWindow = new AlertDetailView(alertEntity, _serviceProvider);
                    detailWindow.Owner = Application.Current.MainWindow;
                    detailWindow.ShowDialog();
                }
                else
                {
                    MessageBox.Show("Alert not found in database.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                // Log the exception details for debugging
                System.Diagnostics.Debug.WriteLine($"Error in ShowDetail: {ex}");
                MessageBox.Show($"Error opening alert details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanShowDetail()
        {
            return SelectedAlert != null;
        }

        [RelayCommand]
        private void ViewDetails(AlertItem? alertItem)
        {
            ViewAlertDetails(alertItem);
        }

        [RelayCommand]
        private async Task DeleteAlert(AlertItem? alertItem)
        {
            await DeleteAlertAsync(alertItem);
        }

        [RelayCommand]
        private async Task ExportCsv()
        {
            await ExportToCsvAsync();
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
