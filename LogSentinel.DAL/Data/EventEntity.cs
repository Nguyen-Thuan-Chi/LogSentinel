using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LogSentinel.DAL.Data
{
    [Table("Events")]
    [Index(nameof(EventTime))]
    [Index(nameof(EventId))]
    [Index(nameof(User))]
    [Index(nameof(Process))]
    [Index(nameof(Host))]
    public class EventEntity
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public DateTime EventTime { get; set; }

        [MaxLength(256)]
        public string Host { get; set; } = "";

        [MaxLength(256)]
        public string User { get; set; } = "";

        public int? EventId { get; set; }

        [MaxLength(256)]
        public string Provider { get; set; } = "";

        [MaxLength(50)]
        public string Level { get; set; } = "Info"; // Info/Warning/Error/Critical/Alert

        [MaxLength(512)]
        public string Process { get; set; } = "";

        [MaxLength(512)]
        public string ParentProcess { get; set; } = "";

        [MaxLength(256)]
        public string Action { get; set; } = "";

        [MaxLength(512)]
        public string Object { get; set; } = "";

        public string DetailsJson { get; set; } = "{}";

        public string? RawXml { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
