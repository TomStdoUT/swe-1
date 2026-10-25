using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace swe_1.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Polls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Question = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ClosingTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AllowMultiple = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDailyChallenge = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotificationsGenerated = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsResolved = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Polls", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false, collation: "NOCASE"),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    PointsBalance = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PollOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PollId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    text = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PollOptions_Polls_PollId",
                        column: x => x.PollId,
                        principalTable: "Polls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PollTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PollId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PollTags_Polls_PollId",
                        column: x => x.PollId,
                        principalTable: "Polls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PollId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AuthorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AuthorName = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_Polls_PollId",
                        column: x => x.PollId,
                        principalTable: "Polls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCosmetics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCosmetics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCosmetics_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserFollows",
                columns: table => new
                {
                    FollowerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FollowedId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFollows", x => new { x.FollowerId, x.FollowedId });
                    table.ForeignKey(
                        name: "FK_UserFollows_Users_FollowedId",
                        column: x => x.FollowedId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserFollows_Users_FollowerId",
                        column: x => x.FollowerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Votes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PollId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BettedPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEarlyBird = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Votes_Polls_PollId",
                        column: x => x.PollId,
                        principalTable: "Polls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Votes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VoteSelections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VoteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OptionIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoteSelections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoteSelections_Votes_VoteId",
                        column: x => x.VoteId,
                        principalTable: "Votes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_AuthorId",
                table: "Comments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_PollId",
                table: "Comments",
                column: "PollId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TargetUserId_CreatedAt",
                table: "Notifications",
                columns: new[] { "TargetUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PollOptions_PollId_Position",
                table: "PollOptions",
                columns: new[] { "PollId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PollTags_PollId",
                table: "PollTags",
                column: "PollId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCosmetics_UserId_Name",
                table: "UserCosmetics",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserFollows_FollowedId",
                table: "UserFollows",
                column: "FollowedId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Votes_PollId_UserId",
                table: "Votes",
                columns: new[] { "PollId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Votes_UserId",
                table: "Votes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VoteSelections_VoteId_Position",
                table: "VoteSelections",
                columns: new[] { "VoteId", "Position" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PollOptions");

            migrationBuilder.DropTable(
                name: "PollTags");

            migrationBuilder.DropTable(
                name: "UserCosmetics");

            migrationBuilder.DropTable(
                name: "UserFollows");

            migrationBuilder.DropTable(
                name: "VoteSelections");

            migrationBuilder.DropTable(
                name: "Votes");

            migrationBuilder.DropTable(
                name: "Polls");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
