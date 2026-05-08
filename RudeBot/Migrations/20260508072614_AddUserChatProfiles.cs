using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RudeBot.Migrations
{
    /// <inheritdoc />
    public partial class AddUserChatProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserChatProfiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    Profile = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserChatProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserChatProfiles_ChatId",
                table: "UserChatProfiles",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_UserChatProfiles_ChatId_UserId",
                table: "UserChatProfiles",
                columns: new[] { "ChatId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserChatProfiles");
        }
    }
}
