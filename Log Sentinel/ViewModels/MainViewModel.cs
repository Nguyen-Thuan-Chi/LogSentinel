using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;
using Log_Sentinel.Helpers;
using Log_Sentinel.UI;

namespace Log_Sentinel.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private System.Windows.Controls.UserControl? _currentView;
        private int _totalToday;
        private int _warningCount;
        private int _errorCount;

        public System.Windows.Controls.UserControl? CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public int TotalToday
        {
            get => _totalToday;
            set { _totalToday = value; OnPropertyChanged(); }
        }

        public int WarningCount
        {
            get => _warningCount;
            set { _warningCount = value; OnPropertyChanged(); }
        }

        public int ErrorCount
        {
            get => _errorCount;
            set { _errorCount = value; OnPropertyChanged(); }
        }

        public ICommand NavigateDashboardCommand { get; }
        public ICommand NavigateRulesCommand { get; }
        public ICommand NavigateEventsCommand { get; }
        public ICommand NavigateAlertsCommand { get; }
        public ICommand NavigateSettingsCommand { get; }
        public ICommand NavigateHelpCommand { get; }
        public ICommand NavigateAboutCommand { get; }

        public MainViewModel()
        {
            NavigateDashboardCommand = new RelayCommand(_ => NavigateTo("Dashboard"));
            NavigateRulesCommand = new RelayCommand(_ => NavigateTo("Rules"));
            NavigateEventsCommand = new RelayCommand(_ => NavigateTo("Events"));
            NavigateAlertsCommand = new RelayCommand(_ => NavigateTo("Alerts"));
            NavigateSettingsCommand = new RelayCommand(_ => NavigateTo("Settings"));
            NavigateHelpCommand = new RelayCommand(_ => NavigateTo("Help"));
            NavigateAboutCommand = new RelayCommand(_ => NavigateTo("About"));

            // Set initial view to Dashboard
            NavigateTo("Dashboard");
        }

        public void NavigateTo(string viewName)
        {
            CurrentView = viewName switch
            {
                "Dashboard" => new DashboardView(),
                "Rules" => new RuleView(),
                "Events" => new EventsView(),
                "Alerts" => new AlertsView(),
                "Settings" => new SettingsView(),
                "Help" => new HelpView(),
                "About" => new AboutView(),
                _ => CurrentView
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}