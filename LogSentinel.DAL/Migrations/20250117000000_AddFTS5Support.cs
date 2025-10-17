using Microsoft.EntityFrameworkCore.Migrations;

namespace LogSentinel.DAL.Migrations
{
    /// <summary>
    /// Adds SQLite FTS5 (Full-Text Search) virtual table for event search
    /// </summary>
    public partial class AddFTS5Support : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Only create FTS5 table if using SQLite
            migrationBuilder.Sql(@"
                CREATE VIRTUAL TABLE IF NOT EXISTS EventsFTS 
                USING fts5(
                    Id UNINDEXED,
                    Host,
                    User,
                    Process,
                    Action,
                    Object,
                    DetailsJson,
                    RawXml,
                    content=Events,
                    content_rowid=Id
                );
            ", suppressTransaction: true);

            // Create triggers to keep FTS in sync with Events table
            migrationBuilder.Sql(@"
                CREATE TRIGGER IF NOT EXISTS Events_ai AFTER INSERT ON Events BEGIN
                    INSERT INTO EventsFTS(Id, Host, User, Process, Action, Object, DetailsJson, RawXml)
                    VALUES (new.Id, new.Host, new.User, new.Process, new.Action, new.Object, new.DetailsJson, new.RawXml);
                END;
            ", suppressTransaction: true);

            migrationBuilder.Sql(@"
                CREATE TRIGGER IF NOT EXISTS Events_ad AFTER DELETE ON Events BEGIN
                    DELETE FROM EventsFTS WHERE Id = old.Id;
                END;
            ", suppressTransaction: true);

            migrationBuilder.Sql(@"
                CREATE TRIGGER IF NOT EXISTS Events_au AFTER UPDATE ON Events BEGIN
                    UPDATE EventsFTS 
                    SET Host = new.Host,
                        User = new.User,
                        Process = new.Process,
                        Action = new.Action,
                        Object = new.Object,
                        DetailsJson = new.DetailsJson,
                        RawXml = new.RawXml
                    WHERE Id = old.Id;
                END;
            ", suppressTransaction: true);

            // Populate FTS table with existing data
            migrationBuilder.Sql(@"
                INSERT INTO EventsFTS(Id, Host, User, Process, Action, Object, DetailsJson, RawXml)
                SELECT Id, Host, User, Process, Action, Object, DetailsJson, RawXml
                FROM Events;
            ", suppressTransaction: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Events_ai;", suppressTransaction: true);
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Events_ad;", suppressTransaction: true);
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Events_au;", suppressTransaction: true);
            migrationBuilder.Sql("DROP TABLE IF EXISTS EventsFTS;", suppressTransaction: true);
        }
    }
}
