using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LogSentinel.BUS.Interfaces;
using LogSentinel.BUS.Models;
using LogSentinel.DAL.Data;
using LogSentinel.DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace LogSentinel.BUS.Services
{
    public class AlertService : IAlertService
    {
        private readonly IAlertRepository _alertRepository;
        private readonly ILogger<AlertService> _logger;
        private readonly HttpClient _httpClient;

        public event EventHandler<AlertDto>? AlertCreated;

        public AlertService(IAlertRepository alertRepository, ILogger<AlertService> logger)
        {
            _alertRepository = alertRepository;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        public async Task<AlertEntity> CreateAlertAsync(
            RuleEntity rule, 
            IEnumerable<EventEntity> matchingEvents, 
            string title, 
            string description)
        {
            var eventIds = matchingEvents.Select(e => e.Id).ToList();
            var eventIdsJson = JsonSerializer.Serialize(eventIds);

            var metadata = new
            {
                event_count = eventIds.Count,
                first_event_time = matchingEvents.Min(e => e.EventTime),
                last_event_time = matchingEvents.Max(e => e.EventTime),
                affected_hosts = matchingEvents.Select(e => e.Host).Distinct().ToList(),
                affected_users = matchingEvents.Select(e => e.User).Distinct().ToList()
            };

            var alert = new AlertEntity
            {
                RuleId = rule.Id,
                RuleName = rule.Name,
                Severity = rule.Severity,
                Timestamp = DateTime.UtcNow,
                Title = title,
                Description = description,
                EventIdsJson = eventIdsJson,
                MetadataJson = JsonSerializer.Serialize(metadata)
            };

            var createdAlert = await _alertRepository.AddAsync(alert);
            _logger.LogWarning("Alert created: {Title} (Severity: {Severity})", title, rule.Severity);

            // Raise event for UI notification
            AlertCreated?.Invoke(this, new AlertDto
            {
                Id = createdAlert.Id,
                RuleName = createdAlert.RuleName,
                Severity = createdAlert.Severity,
                Timestamp = createdAlert.Timestamp,
                Title = createdAlert.Title,
                Description = createdAlert.Description,
                IsAcknowledged = false
            });

            return createdAlert;
        }

        public async Task<IEnumerable<AlertDto>> GetRecentAlertsAsync(int minutes = 5)
        {
            var alerts = await _alertRepository.GetRecentAsync(minutes);
            return alerts.Select(a => new AlertDto
            {
                Id = a.Id,
                RuleName = a.RuleName,
                Severity = a.Severity,
                Timestamp = a.Timestamp,
                Title = a.Title,
                Description = a.Description,
                IsAcknowledged = a.IsAcknowledged
            });
        }

        public async Task AcknowledgeAlertAsync(long alertId, string acknowledgedBy)
        {
            var alert = await _alertRepository.GetByIdAsync(alertId);
            if (alert == null) return;

            alert.IsAcknowledged = true;
            alert.AcknowledgedAt = DateTime.UtcNow;
            alert.AcknowledgedBy = acknowledgedBy;

            await _alertRepository.UpdateAsync(alert);
            _logger.LogInformation("Alert {AlertId} acknowledged by {User}", alertId, acknowledgedBy);
        }

        public async Task ExportToJsonAsync(string filePath)
        {
            var alerts = await _alertRepository.GetAllAsync();
            var json = JsonSerializer.Serialize(alerts, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
            _logger.LogInformation("Alerts exported to JSON: {FilePath}", filePath);
        }

        public async Task ExportToCsvAsync(string filePath)
        {
            var alerts = await _alertRepository.GetAllAsync();
            var csv = new StringBuilder();
            csv.AppendLine("Id,RuleName,Severity,Timestamp,Title,Description,IsAcknowledged");

            foreach (var alert in alerts)
            {
                csv.AppendLine($"{alert.Id},\"{alert.RuleName}\",{alert.Severity},{alert.Timestamp:O},\"{alert.Title}\",\"{alert.Description}\",{alert.IsAcknowledged}");
            }

            await File.WriteAllTextAsync(filePath, csv.ToString());
            _logger.LogInformation("Alerts exported to CSV: {FilePath}", filePath);
        }

        public async Task SendWebhookAsync(AlertEntity alert, string webhookUrl)
        {
            if (string.IsNullOrEmpty(webhookUrl)) return;

            try
            {
                var payload = new
                {
                    alert_id = alert.Id,
                    rule_name = alert.RuleName,
                    severity = alert.Severity,
                    timestamp = alert.Timestamp,
                    title = alert.Title,
                    description = alert.Description
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(webhookUrl, content);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Webhook sent for alert {AlertId} to {Url}", alert.Id, webhookUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send webhook for alert {AlertId}", alert.Id);
            }
        }
    }
}
