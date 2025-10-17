using System.Windows;
using Log_Sentinel.ViewModels;

namespace Log_Sentinel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _mainViewModel;
        private readonly EventsViewModel _eventsViewModel;
        private readonly SettingsViewModel _settingsViewModel;

        // Expose Settings as a bindable property for XAML
        public SettingsViewModel GlobalSettings
        {
            get => _settingsViewModel;
        }

        public MainWindow(MainViewModel mainViewModel, EventsViewModel eventsViewModel, SettingsViewModel settingsViewModel)
        {
            InitializeComponent();
            
            _mainViewModel = mainViewModel;
            _eventsViewModel = eventsViewModel;
            _settingsViewModel = settingsViewModel;
            DataContext = _mainViewModel;
            
            // Bind data to DataGrids
            SystemLogsGrid.ItemsSource = _eventsViewModel.SystemLogs;
            EventReportsGrid.ItemsSource = _eventsViewModel.EventReports;
            
            // No need to add to Resources; use GlobalSettings property for bindings
        }

        private void MainNavListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // 0: Dashboard, 1: Log Viewer, others: keep current
            if (DashboardPanel == null || LogViewerPanel == null)
                return;

            var listBox = sender as System.Windows.Controls.ListBox;
            if (listBox == null)
                return;

            switch (listBox.SelectedIndex)
            {
                case 0:
                    DashboardPanel.Visibility = Visibility.Visible;
                    LogViewerPanel.Visibility = Visibility.Collapsed;
                    break;
                case 1:
                    DashboardPanel.Visibility = Visibility.Collapsed;
                    LogViewerPanel.Visibility = Visibility.Visible;
                    break;
                default:
                    break;
            }
        }
    }
}

