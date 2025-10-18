using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LogSentinel.BUS.Interfaces;
using LogSentinel.BUS.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace LogSentinel.BUS.Services
{
    public class WindowsEventSource : IWindowsEventSource
    {
        private readonly IEventNormalizer _normalizer;
        private readonly ILogger<WindowsEventSource> _logger;
        private readonly IConfiguration _configuration;
        private readonly AsyncRetryPolicy _retryPolicy;
        
        private long _totalEventsRead;
        private long _eventsDropped;
        private long _errorCount;
        private string _status = "Idle";
        private string? _lastError;
        private readonly HashSet<string> _processedEventIds = new();
        private readonly object _statsLock = new();
        
        private readonly string[] _defaultChannels = new[]
        {
            "Security",
            "System", 
            "Application"
        };
        
        private const string SysmonChannel = "Microsoft-Windows-Sysmon/Operational";
        
        public WindowsEventSource(
            IEventNormalizer normalizer,
            ILogger<WindowsEventSource> logger,
            IConfiguration configuration)
        {
            _normalizer = normalizer;
            _logger = logger;
            _configuration = configuration;
            
            // Configure Polly retry policy with exponential backoff
            _retryPolicy = Policy
                .Handle<EventLogException>()
                .Or<UnauthorizedAccessException>()
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            "Retry {RetryCount} after {TimeSpan}s due to {Exception}",
                            retryCount, timeSpan.TotalSeconds, exception.GetType().Name);
                    });
        }
        
        public async Task StartStreamingAsync(ChannelWriter<EventDto> eventChannel, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Windows Event Log streaming...");
            _status = "Starting";
            
            var enableEventLog = _configuration.GetValue<bool>("LogSentinel:Sources:EventLog", true);
            var enableSysmon = _configuration.GetValue<bool>("LogSentinel:Sources:Sysmon", true);
            
            if (!enableEventLog && !enableSysmon)
            {
                _logger.LogWarning("All Windows event sources are disabled in configuration");
                _status = "Disabled";
                return;
            }
            
            var tasks = new List<Task>();
            var activeChannels = new List<string>();
            
            // Start watchers for standard event log channels
            if (enableEventLog)
            {
                foreach (var channelName in _defaultChannels)
                {
                    if (IsChannelAvailable(channelName))
                    {
                        activeChannels.Add(channelName);
                        tasks.Add(WatchChannelAsync(channelName, eventChannel, "WindowsEventLog", cancellationToken));
                    }
                    else
                    {
                        _logger.LogWarning("Event log channel '{Channel}' is not available", channelName);
                    }
                }
            }
            
            // Start watcher for Sysmon if enabled and available
            if (enableSysmon && IsChannelAvailable(SysmonChannel))
            {
                activeChannels.Add(SysmonChannel);
                tasks.Add(WatchChannelAsync(SysmonChannel, eventChannel, "Sysmon", cancellationToken));
            }
            else if (enableSysmon)
            {
                _logger.LogInformation(
                    "Sysmon channel '{Channel}' not found. Sysmon may not be installed. Install from https://docs.microsoft.com/sysinternals/downloads/sysmon",
                    SysmonChannel);
            }
            
            if (tasks.Count == 0)
            {
                _logger.LogError("No event log channels are available. Check permissions or run as Administrator.");
                _status = "No Channels Available";
                _lastError = "No event log channels available. Run as Administrator or grant ETW access.";
                return;
            }
            
            _status = $"Running ({activeChannels.Count} channels)";
            _logger.LogInformation("Streaming from {Count} channels: {Channels}", activeChannels.Count, string.Join(", ", activeChannels));
            
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Event streaming cancelled");
                _status = "Stopped";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in event streaming");
                _status = "Error";
                _lastError = ex.Message;
                throw;
            }
        }
        
        private async Task WatchChannelAsync(
            string channelName,
            ChannelWriter<EventDto> eventChannel,
            string source,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting watcher for channel: {Channel}", channelName);
            
            await _retryPolicy.ExecuteAsync(async () =>
            {
                try
                {
                    var query = new EventLogQuery(channelName, PathType.LogName)
                    {
                        ReverseDirection = false // Read oldest to newest
                    };
                    
                    using var watcher = new EventLogWatcher(query);
                    
                    watcher.EventRecordWritten += async (sender, args) =>
                    {
                        if (args.EventRecord != null)
                        {
                            await ProcessEventRecordAsync(args.EventRecord, eventChannel, source, cancellationToken);
                        }
                    };
                    
                    watcher.Enabled = true;
                    _logger.LogInformation("Watcher enabled for channel: {Channel}", channelName);
                    
                    // Keep the watcher alive until cancellation
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                }
                catch (EventLogNotFoundException ex)
                {
                    _logger.LogWarning("Event log '{Channel}' not found: {Message}", channelName, ex.Message);
                    throw;
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogError(
                        "Insufficient permissions to read '{Channel}'. Run as Administrator. Error: {Message}",
                        channelName, ex.Message);
                    
                    lock (_statsLock)
                    {
                        _errorCount++;
                        _lastError = $"Access denied to {channelName}. Run as Administrator.";
                    }
                    throw;
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Watcher for '{Channel}' cancelled", channelName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error watching channel '{Channel}'", channelName);
                    
                    lock (_statsLock)
                    {
                        _errorCount++;
                        _lastError = $"Error in {channelName}: {ex.Message}";
                    }
                    throw;
                }
            });
        }
        
        private async Task ProcessEventRecordAsync(
            EventRecord eventRecord,
            ChannelWriter<EventDto> eventChannel,
            string source,
            CancellationToken cancellationToken)
        {
            try
            {
                // Generate unique event ID for deduplication
                var uniqueId = $"{eventRecord.LogName}_{eventRecord.RecordId}_{eventRecord.TimeCreated?.Ticks}";
                
                lock (_statsLock)
                {
                    // Simple deduplication - keep last 10,000 IDs
                    if (_processedEventIds.Contains(uniqueId))
                    {
                        return;
                    }
                    
                    _processedEventIds.Add(uniqueId);
                    if (_processedEventIds.Count > 10000)
                    {
                        // Remove oldest half
                        var toRemove = _processedEventIds.Take(5000).ToList();
                        foreach (var id in toRemove)
                        {
                            _processedEventIds.Remove(id);
                        }
                    }
                }
                
                // Normalize the event based on source type
                EventDto eventDto;
                if (source == "Sysmon")
                {
                    eventDto = _normalizer.NormalizeFromSysmonEvent(eventRecord);
                }
                else
                {
                    eventDto = _normalizer.NormalizeFromWindowsEvent(eventRecord);
                }
                
                // Try to write to channel with timeout to prevent blocking
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(5));
                
                var written = await eventChannel.WaitToWriteAsync(cts.Token);
                if (written)
                {
                    await eventChannel.WriteAsync(eventDto, cts.Token);
                    
                    lock (_statsLock)
                    {
                        _totalEventsRead++;
                    }
                }
                else
                {
                    lock (_statsLock)
                    {
                        _eventsDropped++;
                    }
                    _logger.LogWarning("Event channel is full, dropping event from {Source}", source);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event record from {Source}", source);
                lock (_statsLock)
                {
                    _errorCount++;
                }
            }
            finally
            {
                eventRecord?.Dispose();
            }
        }
        
        private bool IsChannelAvailable(string channelName)
        {
            try
            {
                using var session = new EventLogSession();
                var config = session.GetLogInformation(channelName, PathType.LogName);
                return config != null;
            }
            catch (EventLogNotFoundException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Access denied checking channel '{Channel}'. May need Administrator privileges.", channelName);
                return true; // Assume it exists but we need permissions
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking channel '{Channel}'", channelName);
                return false;
            }
        }
        
        public EventSourceStatistics GetStatistics()
        {
            lock (_statsLock)
            {
                var enableEventLog = _configuration.GetValue<bool>("LogSentinel:Sources:EventLog", true);
                var enableSysmon = _configuration.GetValue<bool>("LogSentinel:Sources:Sysmon", true);
                
                var activeChannels = new List<string>();
                if (enableEventLog)
                {
                    activeChannels.AddRange(_defaultChannels.Where(IsChannelAvailable));
                }
                if (enableSysmon && IsChannelAvailable(SysmonChannel))
                {
                    activeChannels.Add(SysmonChannel);
                }
                
                return new EventSourceStatistics
                {
                    TotalEventsRead = _totalEventsRead,
                    EventsDropped = _eventsDropped,
                    ErrorCount = _errorCount,
                    Status = _status,
                    ActiveChannels = activeChannels.ToArray(),
                    LastError = _lastError
                };
            }
        }
        
        public bool HasSufficientPermissions()
        {
            try
            {
                // Try to query the Security log as a permission test
                using var session = new EventLogSession();
                var query = new EventLogQuery("Security", PathType.LogName);
                using var reader = new EventLogReader(query);
                
                // Try to read one event
                using var evt = reader.ReadEvent(TimeSpan.FromMilliseconds(100));
                
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (SecurityException)
            {
                return false;
            }
            catch
            {
                // Other errors don't necessarily mean permission issues
                return true;
            }
        }
    }
}
