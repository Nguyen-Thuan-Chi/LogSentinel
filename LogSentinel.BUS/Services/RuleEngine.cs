using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LogSentinel.BUS.Interfaces;
using LogSentinel.BUS.Models;
using LogSentinel.DAL.Data;
using LogSentinel.DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace LogSentinel.BUS.Services
{
    public class RuleEngine : IRuleEngine
    {
        private readonly IRuleProvider _ruleProvider;
        private readonly IEventRepository _eventRepository;
        private readonly IRuleRepository _ruleRepository;
        private readonly IAlertService _alertService;
        private readonly ILogger<RuleEngine> _logger;
        private List<CompiledRule> _compiledRules = new();

        public RuleEngine(
            IRuleProvider ruleProvider,
            IEventRepository eventRepository,
            IRuleRepository ruleRepository,
            IAlertService alertService,
            ILogger<RuleEngine> logger)
        {
            _ruleProvider = ruleProvider;
            _eventRepository = eventRepository;
            _ruleRepository = ruleRepository;
            _alertService = alertService;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            await ReloadRulesAsync();
        }

        public async Task ReloadRulesAsync()
        {
            var rules = await _ruleProvider.LoadRulesAsync();
            _compiledRules = rules.Select(CompileRule).ToList();
            _logger.LogInformation("Loaded {Count} rules for evaluation", _compiledRules.Count);
            
            // Log each rule for debugging
            foreach (var rule in _compiledRules)
            {
                _logger.LogInformation("Rule loaded: {RuleName} (Severity: {Severity})", 
                    rule.Definition.Name, rule.Definition.Severity);
            }
        }

        public async Task<bool> EvaluateEventAsync(EventEntity evt)
        {
            bool triggered = false;

            _logger.LogDebug("Evaluating event {EventId} against {RuleCount} rules. Process: {Process}, Action: {Action}", 
                evt.Id, _compiledRules.Count, evt.Process, evt.Action);

            foreach (var compiledRule in _compiledRules)
            {
                try
                {
                    if (await EvaluateRuleAsync(compiledRule, evt))
                    {
                        triggered = true;
                        _logger.LogInformation("Rule {RuleName} triggered by event {EventId}", 
                            compiledRule.Definition.Name, evt.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error evaluating rule {RuleName} against event {EventId}", 
                        compiledRule.Definition.Name, evt.Id);
                }
            }

            if (!triggered)
            {
                _logger.LogDebug("No rules triggered for event {EventId} (Process: {Process})", evt.Id, evt.Process);
            }

            return triggered;
        }

        public async Task<IEnumerable<EventEntity>> EvaluateBatchAsync(DateTime startTime, DateTime endTime)
        {
            var matchingEvents = new List<EventEntity>();

            foreach (var compiledRule in _compiledRules)
            {
                var matches = await EvaluateBatchRuleAsync(compiledRule, startTime, endTime);
                matchingEvents.AddRange(matches);
            }

            return matchingEvents;
        }

        private CompiledRule CompileRule(RuleDefinition definition)
        {
            Func<EventEntity, bool> predicate = evt =>
            {
                // Selection criteria
                if (definition.Selection != null)
                {
                    if (definition.Selection.EventId.HasValue && evt.EventId != definition.Selection.EventId.Value)
                        return false;

                    if (!string.IsNullOrEmpty(definition.Selection.Level) && 
                        !string.Equals(evt.Level, definition.Selection.Level, StringComparison.OrdinalIgnoreCase))
                        return false;

                    if (!string.IsNullOrEmpty(definition.Selection.Provider) && 
                        !evt.Provider.Contains(definition.Selection.Provider, StringComparison.OrdinalIgnoreCase))
                        return false;

                    if (!string.IsNullOrEmpty(definition.Selection.Process) && 
                        !evt.Process.Contains(definition.Selection.Process, StringComparison.OrdinalIgnoreCase))
                        return false;

                    if (!string.IsNullOrEmpty(definition.Selection.User) && 
                        !evt.User.Contains(definition.Selection.User, StringComparison.OrdinalIgnoreCase))
                        return false;

                    if (!string.IsNullOrEmpty(definition.Selection.Host) && 
                        !evt.Host.Contains(definition.Selection.Host, StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                // Condition criteria
                if (definition.Condition != null)
                {
                    if (definition.Condition.Always)
                        return true;

                    if (!string.IsNullOrEmpty(definition.Condition.Pattern))
                    {
                        var field = definition.Condition.Field?.ToLower() switch
                        {
                            "details_json" => evt.DetailsJson,
                            "action" => evt.Action ?? "",
                            "process" => evt.Process,
                            _ => evt.DetailsJson
                        };

                        var regex = new Regex(definition.Condition.Pattern, RegexOptions.IgnoreCase);
                        return regex.IsMatch(field);
                    }
                }

                return true;
            };

            return new CompiledRule
            {
                Definition = definition,
                Predicate = predicate
            };
        }

        private async Task<bool> EvaluateRuleAsync(CompiledRule rule, EventEntity evt)
        {
            // Check if event matches selection and condition
            if (!rule.Predicate(evt))
                return false;

            // Handle threshold-based rules (count/timeframe)
            if (rule.Definition.Condition?.Count > 0 && rule.Definition.Condition?.Timeframe > 0)
            {
                var startTime = DateTime.UtcNow.AddSeconds(-(rule.Definition.Condition.Timeframe ?? 300));
                var events = await _eventRepository.GetByDateRangeAsync(startTime, DateTime.UtcNow);
                
                var matchingEvents = events.Where(rule.Predicate).ToList();

                if (rule.Definition.Condition.GroupBy != null)
                {
                    var groups = rule.Definition.Condition.GroupBy.ToLower() switch
                    {
                        "user" => matchingEvents.GroupBy(e => e.User),
                        "host" => matchingEvents.GroupBy(e => e.Host),
                        "process" => matchingEvents.GroupBy(e => e.Process),
                        _ => matchingEvents.GroupBy(e => e.User)
                    };

                    foreach (var group in groups)
                    {
                        if (group.Count() >= rule.Definition.Condition.Count)
                        {
                            await TriggerAlertAsync(rule, group.ToList());
                            return true;
                        }
                    }
                }
                else if (matchingEvents.Count >= rule.Definition.Condition.Count)
                {
                    await TriggerAlertAsync(rule, matchingEvents);
                    return true;
                }
            }
            else
            {
                // Immediate trigger
                await TriggerAlertAsync(rule, new List<EventEntity> { evt });
                return true;
            }

            return false;
        }

        private async Task<IEnumerable<EventEntity>> EvaluateBatchRuleAsync(CompiledRule rule, DateTime startTime, DateTime endTime)
        {
            var events = await _eventRepository.GetByDateRangeAsync(startTime, endTime);
            return events.Where(rule.Predicate).ToList();
        }

        private async Task TriggerAlertAsync(CompiledRule rule, List<EventEntity> matchingEvents)
        {
            var ruleEntity = await _ruleRepository.GetByNameAsync(rule.Definition.Name);
            if (ruleEntity == null) 
            {
                _logger.LogWarning("Rule entity not found for rule: {RuleName}", rule.Definition.Name);
                return;
            }

            var title = rule.Definition.Action?.Title ?? $"Alert: {rule.Definition.Name}";
            var description = rule.Definition.Action?.Description ?? rule.Definition.Description;

            // Replace placeholders
            if (matchingEvents.Any())
            {
                var evt = matchingEvents.First();
                title = title.Replace("{user}", evt.User)
                    .Replace("{host}", evt.Host)
                    .Replace("{count}", matchingEvents.Count.ToString())
                    .Replace("{process}", evt.Process);

                description = description.Replace("{user}", evt.User)
                    .Replace("{host}", evt.Host)
                    .Replace("{count}", matchingEvents.Count.ToString())
                    .Replace("{process}", evt.Process);
            }

            _logger.LogInformation("Creating alert for rule {RuleName}: {Title}", rule.Definition.Name, title);

            var alert = await _alertService.CreateAlertAsync(ruleEntity, matchingEvents, title, description);

            // Update rule trigger stats
            ruleEntity.LastTriggeredAt = DateTime.UtcNow;
            ruleEntity.TriggerCount++;
            await _ruleRepository.UpdateAsync(ruleEntity);

            _logger.LogInformation("Alert {AlertId} created successfully for rule {RuleName}", alert.Id, rule.Definition.Name);
        }

        private class CompiledRule
        {
            public RuleDefinition Definition { get; set; } = null!;
            public Func<EventEntity, bool> Predicate { get; set; } = null!;
        }
    }
}
