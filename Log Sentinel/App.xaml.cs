using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using LogSentinel.BUS.Interfaces;
using LogSentinel.BUS.Services;
using LogSentinel.DAL.Data;
using LogSentinel.DAL.Repositories;
using Log_Sentinel.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Threading;
using System.Threading.Tasks;

namespace Log_Sentinel
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;
        private CancellationTokenSource? _cancellationTokenSource;

        public App()
        {
            // Ensure log directory exists and configure Serilog
            var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(logDir);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(Path.Combine(logDir, "logsentinel-.log"), rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Build configuration using a stable base path for WPF
                var basePath = AppContext.BaseDirectory;
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(basePath)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();

                // Build the host
                _host = Host.CreateDefaultBuilder()
                    .UseSerilog()
                    .ConfigureServices((context, services) =>
                    {
                        // Register Configuration
                        services.AddSingleton<IConfiguration>(configuration);

                        // Resolve connection string or fallback to local SQLite
                        var connectionString = configuration.GetConnectionString("DefaultConnection");
                        if (string.IsNullOrWhiteSpace(connectionString))
                        {
                            var appDataDir = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                "LogSentinel");

                            Directory.CreateDirectory(appDataDir);
                            var dbPath = Path.Combine(appDataDir, "logsentinel.db");
                            connectionString = $"Data Source={dbPath}";
                            Log.Warning("Connection string 'DefaultConnection' not found. Falling back to SQLite at {DbPath}", dbPath);
                        }

                        // Decide provider
                        var useSqlServer =
                            connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
                            connectionString.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase);

                        if (useSqlServer)
                        {
                            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
                            Log.Information("Using SQL Server database");
                        }
                        else
                        {
                            // Ensure SQLite directory exists if path is file based
                            string? sqliteFilePath = null;
                            foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                            {
                                if (part.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                                {
                                    sqliteFilePath = part.Substring("Data Source=".Length).Trim();
                                    break;
                                }
                                if (part.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase))
                                {
                                    sqliteFilePath = part.Substring("Filename=".Length).Trim();
                                    break;
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(sqliteFilePath))
                            {
                                var sqliteDir = Path.GetDirectoryName(sqliteFilePath);
                                if (!string.IsNullOrWhiteSpace(sqliteDir))
                                {
                                    Directory.CreateDirectory(sqliteDir);
                                }
                            }

                            services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
                            Log.Information("Using SQLite database");
                        }

                        // Register Repositories
                        services.AddScoped<IEventRepository, EventRepository>();
                        services.AddScoped<IAlertRepository, AlertRepository>();
                        services.AddScoped<IRuleRepository, RuleRepository>();

                        // Register Services
                        services.AddSingleton<IEventNormalizer, EventNormalizer>();
                        services.AddScoped<IRuleProvider, RuleProvider>();
                        services.AddScoped<IRuleEngine, RuleEngine>();
                        services.AddScoped<IAlertService, AlertService>();
                        services.AddScoped<IEventImporter, EventImporter>();

                        // Register ViewModels
                        services.AddSingleton<MainViewModel>();
                        services.AddSingleton<EventsViewModel>(provider => new EventsViewModel(provider));
                        services.AddSingleton<SettingsViewModel>();

                        // Register MainWindow
                        services.AddSingleton(provider => new MainWindow(
                            provider.GetRequiredService<MainViewModel>(),
                            provider.GetRequiredService<EventsViewModel>(),
                            provider.GetRequiredService<SettingsViewModel>()));
                    })
                    .Build();

                await _host.StartAsync();

                // Seed database
                using (var scope = _host.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await SeedData.SeedDatabaseAsync(dbContext);
                    Log.Information("Database seeded successfully");
                }

                // Initialize Rule Engine
                using (var scope = _host.Services.CreateScope())
                {
                    var ruleEngine = scope.ServiceProvider.GetRequiredService<IRuleEngine>();
                    await ruleEngine.InitializeAsync();
                    Log.Information("Rule engine initialized");
                }

                // Start Event Importer (background worker) - resolve scoped service within its own scope
                _cancellationTokenSource = new CancellationTokenSource();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _host!.Services.CreateScope();
                        var importer = scope.ServiceProvider.GetRequiredService<IEventImporter>();
                        await importer.StartStreamingAsync(_cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // expected during shutdown
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error in event importer");
                    }
                });

                // Show MainWindow
                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                mainWindow.Show();

                Log.Information("LogSentinel application started successfully");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application startup failed");
                MessageBox.Show($"Application startup failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                // Cancel background tasks
                _cancellationTokenSource?.Cancel();

                // Stop the host
                if (_host != null)
                {
                    await _host.StopAsync(TimeSpan.FromSeconds(5));
                    _host.Dispose();
                }

                Log.Information("LogSentinel application stopped");
                Log.CloseAndFlush();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during application shutdown");
            }

            base.OnExit(e);
        }
    }
}

