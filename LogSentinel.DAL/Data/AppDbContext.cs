using Microsoft.EntityFrameworkCore;

namespace LogSentinel.DAL.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<EventEntity> Events { get; set; } = null!;
        public DbSet<AlertEntity> Alerts { get; set; } = null!;
        public DbSet<RuleEntity> Rules { get; set; } = null!;

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure EventEntity for SQLite
            modelBuilder.Entity<EventEntity>(entity =>
            {
                entity.Property(e => e.EventTime).HasColumnType("TEXT");
                entity.Property(e => e.CreatedAt).HasColumnType("TEXT");
            });

            // Configure AlertEntity for SQLite
            modelBuilder.Entity<AlertEntity>(entity =>
            {
                entity.Property(e => e.Timestamp).HasColumnType("TEXT");
                entity.Property(e => e.AcknowledgedAt).HasColumnType("TEXT");
                
                entity.HasOne(a => a.Rule)
                    .WithMany(r => r.Alerts)
                    .HasForeignKey(a => a.RuleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure RuleEntity for SQLite
            modelBuilder.Entity<RuleEntity>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasColumnType("TEXT");
                entity.Property(e => e.UpdatedAt).HasColumnType("TEXT");
                entity.Property(e => e.LastTriggeredAt).HasColumnType("TEXT");
            });

            // SQLite FTS5 virtual table for full-text search
            // Note: This needs to be created via raw SQL after migrations
            // We'll handle this in the migration or seed data
        }
    }
}

