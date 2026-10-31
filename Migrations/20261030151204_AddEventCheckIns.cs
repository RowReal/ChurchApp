using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddEventCheckIns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventCheckIns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChurchEventId = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkerId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EventTableId = table.Column<int>(type: "INTEGER", nullable: false),
                    SeatNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    EventMenuOptionId = table.Column<int>(type: "INTEGER", nullable: false),
                    CheckedInAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MenuUpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsLeadershipSeat = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventCheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventCheckIns_ChurchEvents_ChurchEventId",
                        column: x => x.ChurchEventId,
                        principalTable: "ChurchEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventCheckIns_EventMenuOptions_EventMenuOptionId",
                        column: x => x.EventMenuOptionId,
                        principalTable: "EventMenuOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventCheckIns_EventTables_EventTableId",
                        column: x => x.EventTableId,
                        principalTable: "EventTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventCheckIns_ChurchEventId_CheckedInAtUtc",
                table: "EventCheckIns",
                columns: new[] { "ChurchEventId", "CheckedInAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EventCheckIns_ChurchEventId_WorkerId",
                table: "EventCheckIns",
                columns: new[] { "ChurchEventId", "WorkerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventCheckIns_EventMenuOptionId",
                table: "EventCheckIns",
                column: "EventMenuOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_EventCheckIns_EventTableId_SeatNumber",
                table: "EventCheckIns",
                columns: new[] { "EventTableId", "SeatNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventCheckIns");
        }
    }
}
