using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using LogSentinel.DAL.Data;
using LogSentinel.DAL.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace Log_Sentinel.ViewModels
{
    public partial class AlertDetailViewModel : ObservableObject
    {
        private readonly IServiceProvider? _serviceProvider;
        
        [ObservableProperty]
        private AlertEntity? selectedAlert;

        [ObservableProperty]
        private EventEntity? triggeringEvent;

        [ObservableProperty]
        private bool isLoading = true;

        [ObservableProperty]
        private string? errorMessage;

        public AlertDetailViewModel(AlertEntity alertEntity, IServiceProvider? serviceProvider = null)
        {
            _serviceProvider = serviceProvider;
            SelectedAlert = alertEntity;
            // Don't call async method from constructor - will be called from InitializeAsync
        }

        /// <summary>
        /// Initializes the ViewModel asynchronously. Call this after creating the instance.
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                if (_serviceProvider == null || SelectedAlert == null)
                {
                    ErrorMessage = "Unable to load alert details: Missing dependencies";
                    IsLoading = false;
                    return;
                }

                // Parse the EventIdsJson to get the first event ID
                try
                {
                    var eventIds = System.Text.Json.JsonSerializer.Deserialize<long[]>(SelectedAlert.EventIdsJson ?? "[]");
                    
                    if (eventIds != null && eventIds.Length > 0)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
                        
                        // Get the first triggering event
                        var eventEntity = await eventRepository.GetByIdAsync(eventIds[0]);
                        
                        if (eventEntity == null)
                        {
                            ErrorMessage = "The triggering event could not be found in the database";
                        }
                        else
                        {
                            // Update properties safely on UI thread
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                TriggeringEvent = eventEntity;
                            });
                        }
                    }
                    else
                    {
                        ErrorMessage = "No triggering events found for this alert";
                    }
                }
                catch (System.Text.Json.JsonException jsonEx)
                {
                    ErrorMessage = "Invalid event data format in alert";
                    System.Diagnostics.Debug.WriteLine($"JSON parsing error: {jsonEx.Message}");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading triggering event: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error in InitializeAsync: {ex}");
            }
            finally
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = false;
                });
            }
        }

        // Formatted properties for display
        public string FormattedTimestamp => SelectedAlert?.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "N/A";
        
        public string FormattedAcknowledgedAt => SelectedAlert?.AcknowledgedAt?.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "Not acknowledged";
        
        public string StatusDisplay => SelectedAlert?.IsAcknowledged == true ? "✅ Acknowledged" : "🔴 New";
        
        public string AcknowledgedByDisplay => SelectedAlert?.IsAcknowledged == true 
            ? SelectedAlert.AcknowledgedBy ?? "Unknown" 
            : "Not acknowledged";

        public string EventTimeFormatted => TriggeringEvent?.EventTime.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "N/A";
        
        public string EventDetailsFormatted
        {
            get
            {
                if (TriggeringEvent == null) return "N/A";
                
                try
                {
                    // Try to format JSON nicely
                    if (string.IsNullOrWhiteSpace(TriggeringEvent.DetailsJson))
                        return "{}";
                        
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(TriggeringEvent.DetailsJson);
                    return System.Text.Json.JsonSerializer.Serialize(jsonDoc, new System.Text.Json.JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error formatting JSON: {ex.Message}");
                    return TriggeringEvent.DetailsJson ?? "{}";
                }
            }
        }
    }
}