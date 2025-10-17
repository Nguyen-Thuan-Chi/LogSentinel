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

        public EventImporter(
            IEventNormalizer normalizer,
            IServiceProvider serviceProvider,
            ILogger<EventImporter> logger,
            IConfiguration configuration)
        {
            _normalizer = normalizer;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;

            _channelCapacity = configuration.GetValue<int>("LogSentinel:ChannelCapacity", 10000);
            _eventChannel = Channel.CreateBounded<EventDto>(new BoundedChannelOptions(_channelCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
        }

        public async Task StartStreamingAsync(CancellationToken cancellationToken)
        {
            var sampleLogsPath = _configuration.GetValue<string>("LogSentinel:SampleLogsPath", "./sample-logs/incoming");
            var enableWatcher = _configuration.GetValue<bool>("LogSentinel:EnableFileWatcher", true);

            if (!enableWatcher)
            {
                _logger.LogInformation("File watcher disabled");
                return;
            }

            // Ensure directory exists
            Directory.CreateDirectory(sampleLogsPath);

            // Start background processor
            _ = Task.Run(() => ProcessEventsAsync(cancellationToken), cancellationToken);

            // Start file watcher
            await WatchDirectoryAsync(sampleLogsPath, cancellationToken);
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
