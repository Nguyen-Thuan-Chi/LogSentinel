using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LogSentinel.DAL.Data
{
    [Table("Alerts")]
    [Index(nameof(Timestamp))]
    [Index(nameof(Severity))]
    [Index(nameof(RuleId))]
    public class AlertEntity
    {
        [Key]
        public long Id { get; set; }

        public long RuleId { get; set; }

        [MaxLength(256)]
        [Required]
        public string RuleName { get; set; } = "";

        [MaxLength(50)]
        public string Severity { get; set; } = "Medium"; // Low/Medium/High/Critical

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(1024)]
        public string Title { get; set; } = "";

        public string Description { get; set; } = "";

        public string EventIdsJson { get; set; } = "[]"; // JSON array of event IDs that triggered the alert

        public string MetadataJson { get; set; } = "{}"; // Additional context

        public bool IsAcknowledged { get; set; } = false;

        public DateTime? AcknowledgedAt { get; set; }

        [MaxLength(256)]
        public string? AcknowledgedBy { get; set; }

        // Navigation
        [ForeignKey(nameof(RuleId))]
        public RuleEntity? Rule { get; set; }
    }
}
