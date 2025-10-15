using System.Windows;
using Log_Sentinel.ViewModels;

namespace Log_Sentinel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly EventsViewModel _eventsViewModel;

        public MainWindow()
        {
            InitializeComponent();
            
            _eventsViewModel = new EventsViewModel();
            DataContext = _eventsViewModel;
            
            // Bind data to DataGrids
            SystemLogsGrid.ItemsSource = _eventsViewModel.SystemLogs;
            EventReportsGrid.ItemsSource = _eventsViewModel.EventReports;
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