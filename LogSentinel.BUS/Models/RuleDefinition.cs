namespace LogSentinel.BUS.Models
{
    public class RuleDefinition
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Severity { get; set; } = "Medium";
        public bool Enabled { get; set; } = true;
        public SelectionCriteria? Selection { get; set; }
        public ConditionCriteria? Condition { get; set; }
        public ActionCriteria? Action { get; set; }
    }

    public class SelectionCriteria
    {
        public int? EventId { get; set; }
        public string? Level { get; set; }
        public string? Provider { get; set; }
        public string? Process { get; set; }
        public string? User { get; set; }
        public string? Host { get; set; }
    }

    public class ConditionCriteria
    {
        public int? Count { get; set; }
        public int? Timeframe { get; set; } // seconds
        public string? GroupBy { get; set; }
        public string? Pattern { get; set; }
        public string? Field { get; set; }
        public bool Always { get; set; }
    }

    public class ActionCriteria
    {
        public bool Alert { get; set; } = true;
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
    }
}
