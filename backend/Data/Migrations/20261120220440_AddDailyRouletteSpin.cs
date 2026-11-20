using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace swe_1.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyRouletteSpin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyRouletteSpins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AwardedPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    SpunAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyRouletteSpins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyRouletteSpins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyRouletteSpins_UserId_SpunAtUtc",
                table: "DailyRouletteSpins",
                columns: new[] { "UserId", "SpunAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyRouletteSpins");
        }
    }
}
