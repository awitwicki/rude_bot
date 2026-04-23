using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RudeBot.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRudeCoins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RudeCoins",
                table: "UserStats");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RudeCoins",
                table: "UserStats",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
