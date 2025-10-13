using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Log_Sentinel.ViewModels
{
    // Simple log entry model
    public class LogEntry : INotifyPropertyChanged
    {
        private string _time = string.Empty;
        private string _level = string.Empty;
        private string _message = string.Empty;

        public string Time
        {
            get => _time;
            set { _time = value; OnPropertyChanged(nameof(Time)); }
        }

        public string Level
        {
            get => _level;
            set { _level = value; OnPropertyChanged(nameof(Level)); }
        }

        public string Message
        {
            get => _message;
            set { _message = value; OnPropertyChanged(nameof(Message)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Optimized EventsViewModel
    public class EventsViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<LogEntry> _systemLogs = new();
        private ObservableCollection<LogEntry> _eventReports = new();

        public ObservableCollection<LogEntry> SystemLogs
        {
            get => _systemLogs;
            set { _systemLogs = value; OnPropertyChanged(nameof(SystemLogs)); }
        }

        public ObservableCollection<LogEntry> EventReports
        {
            get => _eventReports;
            set { _eventReports = value; OnPropertyChanged(nameof(EventReports)); }
        }

        public EventsViewModel()
        {
            LoadSampleData();
        }

        private void LoadSampleData()
        {
            // Load sample system logs
            SystemLogs.Add(new LogEntry
            {
                Time = DateTime.Now.AddMinutes(-10).ToString("HH:mm:ss"),
                Level = "Info",
                Message = "Application started successfully"
            });

            SystemLogs.Add(new LogEntry
            {
                Time = DateTime.Now.AddMinutes(-5).ToString("HH:mm:ss"),
                Level = "Warning",
                Message = "Memory usage is high"
            });

            SystemLogs.Add(new LogEntry
            {
                Time = DateTime.Now.AddMinutes(-2).ToString("HH:mm:ss"),
                Level = "Error",
                Message = "Failed to connect to database"
            });

            // Load sample event reports
            EventReports.Add(new LogEntry
            {
                Time = DateTime.Now.AddMinutes(-8).ToString("HH:mm:ss"),
                Level = "Alert",
                Message = "Suspicious login attempt detected"
            });

            EventReports.Add(new LogEntry
            {
                Time = DateTime.Now.AddMinutes(-3).ToString("HH:mm:ss"),
                Level = "Info",
                Message = "Security scan completed"
            });

            EventReports.Add(new LogEntry
            {
                Time = DateTime.Now.AddMinutes(-1).ToString("HH:mm:ss"),
                Level = "Critical",
                Message = "Unauthorized access blocked"
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
