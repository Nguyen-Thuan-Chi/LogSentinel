using System.Collections.Generic;
using System.Threading.Tasks;
using LogSentinel.BUS.Models;
using LogSentinel.DAL.Data;

namespace LogSentinel.BUS.Interfaces
{
    public interface IAlertService
    {
        event System.EventHandler<AlertDto>? AlertCreated;
        Task<AlertEntity> CreateAlertAsync(RuleEntity rule, IEnumerable<EventEntity> matchingEvents, string title, string description);
        Task<IEnumerable<AlertDto>> GetRecentAlertsAsync(int minutes = 5);
        Task AcknowledgeAlertAsync(long alertId, string acknowledgedBy);
        Task ExportToJsonAsync(string filePath);
        Task ExportToCsvAsync(string filePath);
        Task SendWebhookAsync(AlertEntity alert, string webhookUrl);
    }
}
