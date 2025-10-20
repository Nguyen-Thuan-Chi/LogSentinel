using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LogSentinel.BUS.Interfaces;
using LogSentinel.DAL.Data;
using LogSentinel.DAL.Repositories;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;

namespace LogSentinel.BUS.Services
{
    /// <summary>
    /// Simplified dynamic YAML-based rule engine that processes any active rule from the database.
    /// Supports simple key-value YAML format for detection with AND conditions.
    /// </summary>
    public class RuleEngineService : IRuleEngine
    {
        private readonly IRuleRepository _ruleRepository;
        private readonly IAlertService _alertService;
        private readonly ILogger<RuleEngineService> _logger;
        private readonly IDeserializer _yamlDeserializer;

        public RuleEngineService(
            IRuleRepository ruleRepository,
            IAlertService alertService,
            ILogger<RuleEngineService> logger)
        {
            _ruleRepository = ruleRepository;
            _alertService = alertService;
            _logger = logger;

            // Initialize YAML deserializer
            _yamlDeserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();
        }

        /// <summary>
        /// Initialize the rule engine (no-op for this simple implementation)
        /// </summary>
        public async Task InitializeAsync()
        {
            _logger.LogInformation("RuleEngineService initialized");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Reload rules (no-op for this implementation as rules are loaded dynamically)
        /// </summary>
        public async Task ReloadRulesAsync()
        {
            _logger.LogInformation("Rules reloaded (dynamic loading enabled)");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Process a new event against all active rules in the database.
        /// This method replaces the old hardcoded if-else logic.
        /// </summary>
        /// <param name="newEvent">The event to process</param>
        /// <returns>True if any alert was triggered</returns>
        public async Task<bool> ProcessEventAsync(EventEntity newEvent)
        {
            try
            {
                _logger.LogDebug("Processing event {EventId} (Provider: {Provider}, EventId: {EventIdValue})", 
                    newEvent.Id, newEvent.Provider, newEvent.EventId);

                // Load ALL active rules from the database
                var activeRules = await _ruleRepository.GetEnabledAsync();
                var alertTriggered = false;

                // Loop through each rule and check if the event matches
                foreach (var rule in activeRules)
                {
                    try
                    {
                        if (DoesEventMatchRule(newEvent, rule))
                        {
                            // Create alert with information from the rule and event
                            var title = $"Alert: {rule.Name}";
                            var description = $"Rule '{rule.Name}' triggered by event from {newEvent.Host}";

                            await _alertService.CreateAlertAsync(
                                rule, 
                                new[] { newEvent }, 
                                title, 
                                description);

                            // Update rule trigger statistics
                            rule.LastTriggeredAt = DateTime.UtcNow;
                            rule.TriggerCount++;
                            await _ruleRepository.UpdateAsync(rule);

                            _logger.LogInformation("Alert created for rule '{RuleName}' (Severity: {Severity})", 
                                rule.Name, rule.Severity);
                            
                            alertTriggered = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing rule '{RuleName}' for event {EventId}", 
                            rule.Name, newEvent.Id);
                    }
                }

                return alertTriggered;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessEventAsync for event {EventId}", newEvent.Id);
                return false;
            }
        }

        /// <summary>
        /// Core dynamic rule matching logic.
        /// Parses YAML content and performs key-value detection matching.
        /// </summary>
        /// <param name="eventEntity">The event to check</param>
        /// <param name="rule">The rule entity containing YAML content</param>
        /// <returns>True if the event matches all detection criteria</returns>
        private bool DoesEventMatchRule(EventEntity eventEntity, RuleEntity rule)
        {
            try
            {
                // a. Parse the rule's YAML content into a dynamic object
                var yamlObject = _yamlDeserializer.Deserialize<Dictionary<string, object>>(rule.YamlContent);
                
                if (yamlObject == null)
                {
                    _logger.LogWarning("Failed to parse YAML for rule '{RuleName}'", rule.Name);
                    return false;
                }

                // b. Check if the event's Provider and EventId match the rule's 'log_source'
                if (yamlObject.TryGetValue("log_source", out var logSourceObj))
                {
                    var logSource = logSourceObj as Dictionary<object, object>;
                    if (logSource != null)
                    {
                        // Check provider
                        if (logSource.TryGetValue("provider", out var providerObj))
                        {
                            var expectedProvider = providerObj?.ToString();
                            if (!string.IsNullOrEmpty(expectedProvider) && 
                                !eventEntity.Provider.Contains(expectedProvider, StringComparison.OrdinalIgnoreCase))
                            {
                                return false;
                            }
                        }

                        // Check event_id
                        if (logSource.TryGetValue("event_id", out var eventIdObj))
                        {
                            if (int.TryParse(eventIdObj?.ToString(), out var expectedEventId))
                            {
                                if (eventEntity.EventId != expectedEventId)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }

                // c. Deserialize the event's 'DetailsJson' into a Dictionary<string, object>
                Dictionary<string, object>? eventDetails = null;
                if (!string.IsNullOrEmpty(eventEntity.DetailsJson))
                {
                    try
                    {
                        var jsonElement = JsonSerializer.Deserialize<JsonElement>(eventEntity.DetailsJson);
                        eventDetails = JsonElementToDictionary(jsonElement);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse DetailsJson for event {EventId}", eventEntity.Id);
                        eventDetails = new Dictionary<string, object>();
                    }
                }
                else
                {
                    eventDetails = new Dictionary<string, object>();
                }

                // d. Get the 'detection' section from the parsed YAML
                if (!yamlObject.TryGetValue("detection", out var detectionObj))
                {
                    // If no detection section, check if this is a simple rule that should match all events with the log_source
                    _logger.LogDebug("No 'detection' section found in rule '{RuleName}', rule will match based on log_source only", rule.Name);
                    return true; // Changed from false to true to allow log_source-only rules
                }

                var detection = detectionObj as Dictionary<object, object>;
                
                // e. If the 'detection' section is null or empty, return true (match based on log_source)
                if (detection == null || detection.Count == 0)
                {
                    _logger.LogDebug("Detection section is empty for rule '{RuleName}', matching based on log_source", rule.Name);
                    return true; // Changed from false to true
                }

                // f-h. Loop through each key-value pair in the YAML's 'detection' dictionary
                foreach (var kvp in detection)
                {
                    var key = kvp.Key?.ToString();
                    var expectedValue = kvp.Value?.ToString();

                    if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(expectedValue))
                    {
                        continue;
                    }

                    // g. Check if the key exists in the event's JSON dictionary AND if the value matches
                    if (!eventDetails.TryGetValue(key, out var actualValueObj))
                    {
                        // Key not found in event details, rule doesn't match
                        _logger.LogDebug("Key '{Key}' not found in event details for rule '{RuleName}'", 
                            key, rule.Name);
                        return false;
                    }

                    var actualValue = actualValueObj?.ToString();
                    
                    // h. Use case-insensitive string comparison
                    if (!string.Equals(actualValue, expectedValue, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug("Value mismatch for key '{Key}': expected '{Expected}', got '{Actual}' for rule '{RuleName}'", 
                            key, expectedValue, actualValue, rule.Name);
                        return false;
                    }
                }

                // i. If the loop completes without returning false, all conditions matched
                _logger.LogInformation("Rule '{RuleName}' matched for event {EventId} (Provider: {Provider})", 
                    rule.Name, eventEntity.EventId, eventEntity.Provider);
                return true;
            }
            catch (Exception ex)
            {
                // j. Use try-catch blocks for safety during parsing and comparison
                _logger.LogError(ex, "Error evaluating rule '{RuleName}' against event {EventId}", 
                    rule.Name, eventEntity.Id);
                return false;
            }
        }

        /// <summary>
        /// Evaluate event using the new ProcessEventAsync method (compatibility with IRuleEngine)
        /// </summary>
        public async Task<bool> EvaluateEventAsync(EventEntity evt)
        {
            try
            {
                _logger.LogDebug("Evaluating event {EventId} (Provider: {Provider}, EventId: {EventIdValue})", 
                    evt.Id, evt.Provider, evt.EventId);

                // Load ALL active rules from the database
                var activeRules = await _ruleRepository.GetEnabledAsync();
                if (!activeRules.Any())
                {
                    _logger.LogDebug("No active rules found");
                    return false;
                }

                var alertTriggered = false;

                // Loop through each rule and check if the event matches
                foreach (var rule in activeRules)
                {
                    try
                    {
                        if (DoesEventMatchRule(evt, rule))
                        {
                            // Create alert with information from the rule and event
                            var title = $"Alert: {rule.Name}";
                            var description = $"Rule '{rule.Name}' triggered by event from {evt.Host}";

                            await _alertService.CreateAlertAsync(
                                rule, 
                                new[] { evt }, 
                                title, 
                                description);

                            // Update rule trigger statistics
                            rule.LastTriggeredAt = DateTime.UtcNow;
                            rule.TriggerCount++;
                            await _ruleRepository.UpdateAsync(rule);

                            _logger.LogInformation("Alert created for rule '{RuleName}' (Severity: {Severity})", 
                                rule.Name, rule.Severity);
                            
                            alertTriggered = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing rule '{RuleName}' for event {EventId}", 
                            rule.Name, evt.Id);
                    }
                }

                return alertTriggered;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EvaluateEventAsync for event {EventId}", evt.Id);
                return false;
            }
        }

        /// <summary>
        /// Evaluate a batch of events (not implemented for this simple rule engine)
        /// </summary>
        public async Task<IEnumerable<EventEntity>> EvaluateBatchAsync(DateTime startTime, DateTime endTime)
        {
            _logger.LogInformation("Batch evaluation not implemented in simplified rule engine");
            await Task.CompletedTask;
            return new List<EventEntity>();
        }

        /// <summary>
        /// Helper method to convert JsonElement to Dictionary<string, object>
        /// </summary>
        private static Dictionary<string, object> JsonElementToDictionary(JsonElement element)
        {
            var dictionary = new Dictionary<string, object>();

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        dictionary[property.Name] = JsonElementToObject(property.Value);
                    }
                    break;
            }

            return dictionary;
        }

        /// <summary>
        /// Helper method to convert JsonElement to appropriate .NET object
        /// </summary>
        private static object JsonElementToObject(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? "",
                JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null!,
                JsonValueKind.Object => JsonElementToDictionary(element),
                JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToArray(),
                _ => element.ToString()
            };
        }
    }
}