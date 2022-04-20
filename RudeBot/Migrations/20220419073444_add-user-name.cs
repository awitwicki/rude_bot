using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RudeBot.Migrations
{
    public partial class addusername : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserMention",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserMention",
                table: "Users");
        }
    }
}
