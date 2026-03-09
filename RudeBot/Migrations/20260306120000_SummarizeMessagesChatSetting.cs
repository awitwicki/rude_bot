using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RudeBot.Migrations
{
    /// <inheritdoc />
    public partial class SummarizeMessagesChatSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SummarizeMessages",
                table: "ChatSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SummarizeMessages",
                table: "ChatSettings");
        }
    }
}
