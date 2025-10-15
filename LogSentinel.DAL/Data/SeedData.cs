using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogSentinel.DAL.Data
{
    public static class SeedData
    {
        public static void EnsureSeed()
        {
            using var db = new AppDbContext();
            db.Database.EnsureCreated();
            if (!db.Events.Any())
            {
                db.Events.AddRange(
                    new EventEntity { Time = DateTime.Now.AddMinutes(-30), Level = "Info", Host = "HOST1", User = "alice", Process = "svchost", Message = "Application started successfully" },
                    new EventEntity { Time = DateTime.Now.AddMinutes(-15), Level = "Warning", Host = "HOST1", User = "bob", Process = "chrome", Message = "Memory usage is high" },
                    new EventEntity { Time = DateTime.Now.AddMinutes(-10), Level = "Error", Host = "HOST2", User = "eve", Process = "sqlservr", Message = "Failed to connect to database" }
                );
                db.SaveChanges();
            }
        }
    }
}
