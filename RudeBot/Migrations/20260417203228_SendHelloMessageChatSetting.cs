using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RudeBot.Migrations
{
    /// <inheritdoc />
    public partial class SendHelloMessageChatSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SendHelloMessage",
                table: "ChatSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SendHelloMessage",
                table: "ChatSettings");
        }
    }
}
