using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace LogSentinel.DAL.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<EventEntity> Events { get; set; }

        private string _dbPath;
        public AppDbContext()
        {
            _dbPath = "logsentinel_mini.db";
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={_dbPath}");
    }
}
