using System.Windows;
using System.ComponentModel;
using System.IO;
using Log_Sentinel.ViewModels;
using LogSentinel.BUS.Interfaces;
using Hardcodet.Wpf.TaskbarNotification;

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
        private TaskbarIcon? _taskbarIcon;
        private bool _isExiting = false;

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

            // Initialize System Tray
            InitializeSystemTray();

            // Initialize the ComboBox to the current DisplayMode
            if (DisplayModeComboBox != null)
            {
                DisplayModeComboBox.SelectedValue = _settingsViewModel.DisplayMode;
            }

            // Subscribe to alert notifications and show message box
            _alertService.AlertCreated += (s, alert) =>
            {
                // alert is AlertDto type from the event
                Dispatcher.Invoke(() =>
                {
                    var result = System.Windows.MessageBox.Show($"[{alert.Severity}] {alert.Title}\n\n{alert.Description}",
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

        private void DisplayModeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // The binding automatically updates the SettingsViewModel.DisplayMode property
            // Any ViewModels listening to PropertyChanged on SettingsViewModel will be notified
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

        #region System Tray Implementation

        private void InitializeSystemTray()
        {
            _taskbarIcon = new TaskbarIcon();
            
            // Set the icon
            _taskbarIcon.IconSource = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Assets/app_icon.ico"));
            _taskbarIcon.ToolTipText = "Log Sentinel";
            
            // Handle double-click to show window
            _taskbarIcon.TrayMouseDoubleClick += (s, e) => ShowWindow();
            
            // Create context menu
            CreateContextMenu();
            
            _taskbarIcon.Visibility = Visibility.Hidden; // Initially hidden
        }

        private void CreateContextMenu()
        {
            var contextMenu = new System.Windows.Controls.ContextMenu();

            // Open Log Sentinel
            var openMenuItem = new System.Windows.Controls.MenuItem();
            openMenuItem.Header = "Open Log Sentinel";
            openMenuItem.FontWeight = FontWeights.Bold;
            openMenuItem.Click += (s, e) => ShowWindow();
            contextMenu.Items.Add(openMenuItem);

            // Separator
            contextMenu.Items.Add(new System.Windows.Controls.Separator());

            // Configuration (placeholder)
            var configMenuItem = new System.Windows.Controls.MenuItem();
            configMenuItem.Header = "Configuration";
            configMenuItem.Click += (s, e) => 
            {
                // Placeholder - không thực hiện hành động gì
                // Bạn có thể thêm logic sau này
            };
            contextMenu.Items.Add(configMenuItem);

            // Separator
            contextMenu.Items.Add(new System.Windows.Controls.Separator());

            // Exit
            var exitMenuItem = new System.Windows.Controls.MenuItem();
            exitMenuItem.Header = "Exit";
            exitMenuItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(exitMenuItem);

            if(_taskbarIcon != null)
                _taskbarIcon.ContextMenu = contextMenu;
        }

        private void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            if(_taskbarIcon != null)
                _taskbarIcon.Visibility = Visibility.Hidden;
        }

        private void HideToSystemTray()
        {
            Hide();
            if(_taskbarIcon != null)
            {
                _taskbarIcon.Visibility = Visibility.Visible;
                
                // Show balloon tip to inform user
                _taskbarIcon.ShowBalloonTip("Log Sentinel", "Application has been minimized to system tray.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
            }
        }

        private void ExitApplication()
        {
            _isExiting = true;
            _taskbarIcon?.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_isExiting)
            {
                // Cancel the closing and hide to system tray instead
                e.Cancel = true;
                HideToSystemTray();
            }
            else
            {
                // Clean up
                _taskbarIcon?.Dispose();
                base.OnClosing(e);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _taskbarIcon?.Dispose();
            base.OnClosed(e);
        }

        #endregion
    }
}



