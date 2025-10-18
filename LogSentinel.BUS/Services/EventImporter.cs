using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LogSentinel.BUS.Interfaces;
using LogSentinel.BUS.Models;
using LogSentinel.DAL.Data;
using LogSentinel.DAL.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace LogSentinel.BUS.Services
{
    public class EventImporter : IEventImporter
    {
        private readonly IEventNormalizer _normalizer;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EventImporter> _logger;
        private readonly IConfiguration _configuration;
        private readonly Channel<EventDto> _eventChannel;
        private readonly int _channelCapacity;
        private readonly IWindowsEventSource? _windowsEventSource;

        public EventImporter(
            IEventNormalizer normalizer,
            IServiceProvider serviceProvider,
            ILogger<EventImporter> logger,
            IConfiguration configuration,
            IWindowsEventSource? windowsEventSource = null)
        {
            _normalizer = normalizer;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
            _windowsEventSource = windowsEventSource;

            _channelCapacity = configuration.GetValue<int>("LogSentinel:ChannelCapacity", 10000);
            _eventChannel = Channel.CreateBounded<EventDto>(new BoundedChannelOptions(_channelCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
        }

        public async Task StartStreamingAsync(CancellationToken cancellationToken)
        {
            var sampleLogsPath = _configuration.GetValue<string>("LogSentinel:SampleLogsPath", "./sample-logs/incoming");
            var enableFileWatcher = _configuration.GetValue<bool>("LogSentinel:EnableFileWatcher", true);
            var enableSampleFiles = _configuration.GetValue<bool>("LogSentinel:Sources:SampleFiles", true);

            // Start background processor first (shared by all sources)
            _ = Task.Run(() => ProcessEventsAsync(cancellationToken), cancellationToken);

            var tasks = new List<Task>();

            // Start Windows Event Log source if configured
            if (_windowsEventSource != null)
            {
                var enableEventLog = _configuration.GetValue<bool>("LogSentinel:Sources:EventLog", false);
                var enableSysmon = _configuration.GetValue<bool>("LogSentinel:Sources:Sysmon", false);

                if (enableEventLog || enableSysmon)
                {
                    _logger.LogInformation("Starting Windows Event Log and Sysmon streaming");
                    
                    // Check permissions
                    if (!_windowsEventSource.HasSufficientPermissions())
                    {
                        _logger.LogWarning(
                            "Insufficient privileges to read Security channel. Some events may be unavailable. " +
                            "Run as Administrator or grant specific ETW rights for full functionality.");
                    }
                    
                    tasks.Add(_windowsEventSource.StartStreamingAsync(_eventChannel.Writer, cancellationToken));
                }
            }

            // Start file watcher for sample logs if configured
            if (enableFileWatcher && enableSampleFiles)
            {
                _logger.LogInformation("File watcher enabled for sample logs");
                
                // Ensure directory exists
                Directory.CreateDirectory(sampleLogsPath);
                
                tasks.Add(WatchDirectoryAsync(sampleLogsPath, cancellationToken));
            }
            else
            {
                _logger.LogInformation("Sample file watcher disabled");
            }

            if (tasks.Count == 0)
            {
                _logger.LogWarning("No event sources are enabled. Enable at least one source in configuration.");
                return;
            }

            // Wait for all streaming tasks
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Event streaming stopped");
            }
        }

        public async Task ImportBatchAsync(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                _logger.LogWarning("Directory not found: {Path}", directoryPath);
                return;
            }

            var files = Directory.GetFiles(directoryPath, "*.log");
            _logger.LogInformation("Importing {Count} log files from {Path}", files.Length, directoryPath);

            foreach (var file in files)
            {
                await ImportFileAsync(file);
            }
        }

        private async Task WatchDirectoryAsync(string path, CancellationToken cancellationToken)
        {
            using var watcher = new FileSystemWatcher(path)
            {
                Filter = "*.log",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime
            };

            watcher.Changed += async (sender, e) => await OnFileChanged(e.FullPath);
            watcher.Created += async (sender, e) => await OnFileChanged(e.FullPath);

            watcher.EnableRaisingEvents = true;
            _logger.LogInformation("Watching directory: {Path}", path);

            // Keep the watcher alive
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("File watcher stopped");
            }
        }

        private async Task OnFileChanged(string filePath)
        {
            try
            {
                // Wait a bit to ensure file is fully written
                await Task.Delay(100);
                await ImportFileAsync(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file: {FilePath}", filePath);
            }
        }

        private async Task ImportFileAsync(string filePath)
        {
            try
            {
                var lines = await File.ReadAllLinesAsync(filePath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var eventDto = _normalizer.Normalize(line);
                    await _eventChannel.Writer.WriteAsync(eventDto);
                }

                _logger.LogInformation("Imported {Count} events from {File}", lines.Length, Path.GetFileName(filePath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing file: {FilePath}", filePath);
            }
        }

        private async Task ProcessEventsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Event processor started");

            await foreach (var eventDto in _eventChannel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    // Use scoped services for DB operations
                    using var scope = _serviceProvider.CreateScope();
                    var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
                    var ruleEngine = scope.ServiceProvider.GetRequiredService<IRuleEngine>();

                    // Convert DTO to Entity
                    var entity = new EventEntity
                    {
                        EventTime = eventDto.EventTime,
                        Host = eventDto.Host,
                        User = eventDto.User,
                        EventId = eventDto.EventId,
                        Provider = eventDto.Provider,
                        Level = eventDto.Level,
                        Process = eventDto.Process,
                        ParentProcess = eventDto.ParentProcess,
                        Action = eventDto.Action,
                        Object = eventDto.Object,
                        DetailsJson = eventDto.DetailsJson,
                        RawXml = eventDto.RawXml,
                        Source = eventDto.Source,
                        CreatedAt = DateTime.UtcNow
                    };

                    // Persist to database
                    entity = await eventRepository.AddAsync(entity);

                    // Evaluate rules
                    await ruleEngine.EvaluateEventAsync(entity);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing event");
                }
            }

            _logger.LogInformation("Event processor stopped");
        }
    }
}
