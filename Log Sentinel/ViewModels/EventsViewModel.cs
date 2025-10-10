using System;
using System.Collections.ObjectModel;

namespace LogSentinelMini.ViewModels
{
    // Một row trong DataGrid
    public class EventRow
    {
        public long Id { get; set; }            
        public string Time { get; set; }        
        public string Host { get; set; }        
        public string User { get; set; }        
        public int EventId { get; set; }        
        public string Provider { get; set; }    
        public string Process { get; set; }     
        public string Action { get; set; }      
    }

    // ViewModel của EventsView
    public class EventsViewModel
    {
        // ObservableCollection: DataGrid tự động update khi thêm/xóa
        public ObservableCollection<EventRow> Events { get; set; }

        public EventsViewModel()
        {
            Events = new ObservableCollection<EventRow>();
            LoadFakeData();  // Load dữ liệu mẫu ban đầu
        }

        // Hàm load dữ liệu fake
        private void LoadFakeData()
        {
            Events.Clear();

            // Thêm một số event giả lập
            Events.Add(new EventRow
            {
                Id = 1,
                Time = DateTime.UtcNow.AddMinutes(-10).ToString("s"),
                Host = "host1",
                User = "alice",
                EventId = 4625,
                Provider = "Security",
                Process = "ssh.exe",
                Action = "FailedLogon"
            });

            Events.Add(new EventRow
            {
                Id = 2,
                Time = DateTime.UtcNow.AddMinutes(-5).ToString("s"),
                Host = "host2",
                User = "bob",
                EventId = 1,
                Provider = "Sysmon",
                Process = "powershell.exe",
                Action = "ProcessCreate"
            });

            Events.Add(new EventRow
            {
                Id = 3,
                Time = DateTime.UtcNow.AddMinutes(-2).ToString("s"),
                Host = "host3",
                User = "charlie",
                EventId = 4688,
                Provider = "Security",
                Process = "cmd.exe",
                Action = "ProcessCreate"
            });
        }
    }
}
