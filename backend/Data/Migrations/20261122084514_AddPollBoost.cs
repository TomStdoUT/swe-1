using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace swe_1.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPollBoost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBoosted",
                table: "Polls",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBoosted",
                table: "Polls");
        }
    }
}
