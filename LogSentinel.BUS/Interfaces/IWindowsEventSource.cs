using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LogSentinel.BUS.Models;

namespace LogSentinel.BUS.Interfaces
{
    /// <summary>
    /// Provides real-time streaming of Windows Event Log and Sysmon events
    /// </summary>
    public interface IWindowsEventSource
    {
        /// <summary>
        /// Start streaming Windows Event Log and Sysmon events into the provided channel
        /// </summary>
        /// <param name="eventChannel">Bounded channel to write events to</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task StartStreamingAsync(ChannelWriter<EventDto> eventChannel, CancellationToken cancellationToken);
        
        /// <summary>
        /// Get current statistics about event ingestion
        /// </summary>
        EventSourceStatistics GetStatistics();
        
        /// <summary>
        /// Check if the service has sufficient permissions to read event logs
        /// </summary>
        bool HasSufficientPermissions();
    }
    
    public class EventSourceStatistics
    {
        public long TotalEventsRead { get; set; }
        public long EventsDropped { get; set; }
        public long ErrorCount { get; set; }
        public string Status { get; set; } = "Idle";
        public string[] ActiveChannels { get; set; } = Array.Empty<string>();
        public string? LastError { get; set; }
    }
}
