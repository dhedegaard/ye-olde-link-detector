using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace yeoldelinkdetector.Migrations
{
    /// <inheritdoc />
    public partial class GuildEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Guilds",
                columns: table => new
                {
                    GuildId = table.Column<string>(type: "text", nullable: false),
                    LastMessageId = table.Column<string>(type: "text", nullable: false),
                    LastMessageTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guilds", x => x.GuildId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_GuildId",
                table: "Guilds",
                column: "GuildId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Guilds");
        }
    }
}
