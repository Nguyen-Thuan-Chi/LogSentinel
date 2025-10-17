using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LogSentinel.DAL.Data
{
    [Table("Rules")]
    [Index(nameof(Name))]
    [Index(nameof(IsEnabled))]
    public class RuleEntity
    {
        [Key]
        public long Id { get; set; }

        [MaxLength(256)]
        [Required]
        public string Name { get; set; } = "";

        public string Description { get; set; } = "";

        [MaxLength(50)]
        public string Severity { get; set; } = "Medium"; // Low/Medium/High/Critical

        [Required]
        public string YamlContent { get; set; } = "";

        public bool IsEnabled { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? LastTriggeredAt { get; set; }

        public int TriggerCount { get; set; } = 0;

        // Navigation
        public ICollection<AlertEntity> Alerts { get; set; } = new List<AlertEntity>();
    }
}
