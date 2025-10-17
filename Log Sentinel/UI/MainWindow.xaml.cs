using System.Windows;
using Log_Sentinel.ViewModels;
using LogSentinel.BUS.Interfaces;

namespace Log_Sentinel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _mainViewModel;
        private readonly SettingsViewModel _settingsViewModel;
        private readonly IAlertService _alertService;

        // Expose Settings as a bindable property for XAML
        public SettingsViewModel GlobalSettings
        {
            get => _settingsViewModel;
        }

        public MainWindow(MainViewModel mainViewModel, SettingsViewModel settingsViewModel, IAlertService alertService)
        {
            // Assign fields FIRST
            _mainViewModel = mainViewModel;
            _settingsViewModel = settingsViewModel;
            _alertService = alertService;
            
            InitializeComponent(); // Now safe to trigger events
            
            DataContext = _mainViewModel;

            // Subscribe to alert notifications and show message box
            _alertService.AlertCreated += (s, alert) =>
            {
                // alert is AlertDto type from the event
                Dispatcher.Invoke(() =>
                {
                    var result = MessageBox.Show($"[{alert.Severity}] {alert.Title}\n\n{alert.Description}",
                        "New Alert",
                        MessageBoxButton.OKCancel,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.OK && MainNavListBox != null)
                    {
                        MainNavListBox.SelectedIndex = 3; // Navigate to Alerts
                        _mainViewModel.NavigateTo("Alerts");
                    }
                });
            };
        }

        private void MainNavListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var listBox = sender as System.Windows.Controls.ListBox;
            if (listBox == null || listBox.SelectedIndex < 0)
                return;

            // Clear secondary navigation selection when main nav is changed
            if (SecondaryNavListBox != null)
                SecondaryNavListBox.SelectedIndex = -1;

            switch (listBox.SelectedIndex)
            {
                case 0: // Dashboard
                    _mainViewModel.NavigateTo("Dashboard");
                    break;
                case 1: // Log Viewer
                    _mainViewModel.NavigateTo("Events");
                    break;
                case 2: // Rules
                    _mainViewModel.NavigateTo("Rules");
                    break;
                case 3: // Alerts
                    _mainViewModel.NavigateTo("Alerts");
                    break;
                case 4: // Analytics (placeholder)
                    // TODO: Create AnalyticsView
                    break;
                case 5: // Import / Export (placeholder)
                    // TODO: Create ImportExportView
                    break;
            }
        }

        private void SecondaryNavListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var listBox = sender as System.Windows.Controls.ListBox;
            if (listBox == null || listBox.SelectedIndex < 0)
                return;

            // Clear main navigation selection when secondary nav is changed
            if (MainNavListBox != null)
                MainNavListBox.SelectedIndex = -1;

            switch (listBox.SelectedIndex)
            {
                case 0: // Settings
                    _mainViewModel.NavigateTo("Settings");
                    break;
                case 1: // About
                    _mainViewModel.NavigateTo("About");
                    break;
                case 2: // Help
                    _mainViewModel.NavigateTo("Help");
                    break;
            }
        }
    }
}



