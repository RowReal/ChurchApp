using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddEventFunEngagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventFunGames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChurchEventId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Phrase = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    OpensAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosesAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsPublished = table.Column<bool>(type: "INTEGER", nullable: false),
                    QuizSubmissionsOpen = table.Column<bool>(type: "INTEGER", nullable: false),
                    RevealAnswers = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowLeaderboard = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventFunGames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventFunGames_ChurchEvents_ChurchEventId",
                        column: x => x.ChurchEventId,
                        principalTable: "ChurchEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventFunEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventFunGameId = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkerId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    EntryNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    Score = table.Column<int>(type: "INTEGER", nullable: true),
                    Answers = table.Column<string>(type: "TEXT", maxLength: 4, nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventFunEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventFunEntries_EventFunGames_EventFunGameId",
                        column: x => x.EventFunGameId,
                        principalTable: "EventFunGames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventFunQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventFunGameId = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    OptionA = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    OptionB = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    OptionC = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    OptionD = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    CorrectOption = table.Column<string>(type: "TEXT", maxLength: 1, nullable: false),
                    BibleReference = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventFunQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventFunQuestions_EventFunGames_EventFunGameId",
                        column: x => x.EventFunGameId,
                        principalTable: "EventFunGames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventFunEntries_EventFunGameId_EntryNumber",
                table: "EventFunEntries",
                columns: new[] { "EventFunGameId", "EntryNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventFunEntries_EventFunGameId_Score_SubmittedAtUtc",
                table: "EventFunEntries",
                columns: new[] { "EventFunGameId", "Score", "SubmittedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EventFunEntries_EventFunGameId_WorkerId",
                table: "EventFunEntries",
                columns: new[] { "EventFunGameId", "WorkerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventFunGames_ChurchEventId_Kind",
                table: "EventFunGames",
                columns: new[] { "ChurchEventId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_EventFunQuestions_EventFunGameId_Position",
                table: "EventFunQuestions",
                columns: new[] { "EventFunGameId", "Position" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventFunEntries");

            migrationBuilder.DropTable(
                name: "EventFunQuestions");

            migrationBuilder.DropTable(
                name: "EventFunGames");
        }
    }
}
