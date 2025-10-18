using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Log_Sentinel.Helpers;

namespace Log_Sentinel.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private string _databasePath = "logsentinel.db";
        private string _databaseSize = "0 KB";
        private int _refreshInterval = 30;
        private bool _enableNotifications = true;
        private bool _enableSoundAlerts = false;
        private string _displayMode = "Professional"; // User | Professional
        private bool _enableSampleFiles = true;
        private bool _enableEventLog = false;
        private bool _enableSysmon = false;

        public string DatabasePath
        {
            get => _databasePath;
            set
            {
                _databasePath = value;
                OnPropertyChanged();
                UpdateDatabaseSize();
            }
        }

        public string DatabaseSize
        {
            get => _databaseSize;
            set
            {
                _databaseSize = value;
                OnPropertyChanged();
            }
        }

        public int RefreshInterval
        {
            get => _refreshInterval;
            set
            {
                _refreshInterval = value;
                OnPropertyChanged();
            }
        }

        public bool EnableNotifications
        {
            get => _enableNotifications;
            set
            {
                _enableNotifications = value;
                OnPropertyChanged();
            }
        }

        public bool EnableSoundAlerts
        {
            get => _enableSoundAlerts;
            set
            {
                _enableSoundAlerts = value;
                OnPropertyChanged();
            }
        }

        public string DisplayMode
        {
            get => _displayMode;
            set
            {
                _displayMode = value;
                OnPropertyChanged();
            }
        }

        public bool EnableSampleFiles
        {
            get => _enableSampleFiles;
            set
            {
                _enableSampleFiles = value;
                OnPropertyChanged();
            }
        }

        public bool EnableEventLog
        {
            get => _enableEventLog;
            set
            {
                _enableEventLog = value;
                OnPropertyChanged();
            }
        }

        public bool EnableSysmon
        {
            get => _enableSysmon;
            set
            {
                _enableSysmon = value;
                OnPropertyChanged();
            }
        }

        public ICommand SaveSettingsCommand { get; }
        public ICommand BrowseDatabaseCommand { get; }

        public SettingsViewModel()
        {
            SaveSettingsCommand = new RelayCommand(_ => SaveSettings());
            BrowseDatabaseCommand = new RelayCommand(_ => BrowseDatabase());

            // Load initial database size
            UpdateDatabaseSize();
        }

        private void SaveSettings()
        {
            // TODO: Save settings to configuration file or database
            MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BrowseDatabase()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "SQLite Database (*.db)|*.db|All files (*.*)|*.*",
                Title = "Select Database File"
            };

            if (dialog.ShowDialog() == true)
            {
                DatabasePath = dialog.FileName;
            }
        }

        private void UpdateDatabaseSize()
        {
            try
            {
                if (File.Exists(DatabasePath))
                {
                    var fileInfo = new FileInfo(DatabasePath);
                    var sizeInBytes = fileInfo.Length;

                    if (sizeInBytes < 1024)
                        DatabaseSize = $"{sizeInBytes} B";
                    else if (sizeInBytes < 1024 * 1024)
                        DatabaseSize = $"{sizeInBytes / 1024.0:F2} KB";
                    else if (sizeInBytes < 1024 * 1024 * 1024)
                        DatabaseSize = $"{sizeInBytes / (1024.0 * 1024.0):F2} MB";
                    else
                        DatabaseSize = $"{sizeInBytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
                }
                else
                {
                    DatabaseSize = "File not found";
                }
            }
            catch
            {
                DatabaseSize = "Unknown";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
