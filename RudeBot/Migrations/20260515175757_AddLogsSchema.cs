using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RudeBot.Migrations
{
    /// <inheritdoc />
    public partial class AddLogsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Schema for Serilog PostgreSQL sink (logs.app_logs). The sink
            // auto-creates the table on first write but cannot create the schema.
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS logs;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS logs CASCADE;");
        }
    }
}
