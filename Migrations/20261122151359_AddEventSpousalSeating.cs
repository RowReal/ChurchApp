using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddEventSpousalSeating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BricksPrivilegeAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActorWorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    PrivilegeCode = table.Column<string>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BricksPrivilegeAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BricksPrivileges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BricksPrivileges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventSeatReservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChurchEventId = table.Column<int>(type: "INTEGER", nullable: false),
                    EventTableId = table.Column<int>(type: "INTEGER", nullable: false),
                    SeatNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    OwnerWorkerId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SpouseWorkerId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    GuestName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    GuestMealOptionId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsGuest = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReleasedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventSeatReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventSeatReservations_ChurchEvents_ChurchEventId",
                        column: x => x.ChurchEventId,
                        principalTable: "ChurchEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventSeatReservations_EventTables_EventTableId",
                        column: x => x.EventTableId,
                        principalTable: "EventTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BricksUserPrivileges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    BricksPrivilegeId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    AssignedByWorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RevokedByWorkerId = table.Column<int>(type: "INTEGER", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BricksUserPrivileges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BricksUserPrivileges_BricksPrivileges_BricksPrivilegeId",
                        column: x => x.BricksPrivilegeId,
                        principalTable: "BricksPrivileges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BricksUserPrivileges_Workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BricksPrivilegeAudits_WorkerId_OccurredAtUtc",
                table: "BricksPrivilegeAudits",
                columns: new[] { "WorkerId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BricksPrivileges_Code",
                table: "BricksPrivileges",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BricksUserPrivileges_BricksPrivilegeId",
                table: "BricksUserPrivileges",
                column: "BricksPrivilegeId");

            migrationBuilder.CreateIndex(
                name: "IX_BricksUserPrivileges_WorkerId_BricksPrivilegeId",
                table: "BricksUserPrivileges",
                columns: new[] { "WorkerId", "BricksPrivilegeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventSeatReservations_ChurchEventId_OwnerWorkerId",
                table: "EventSeatReservations",
                columns: new[] { "ChurchEventId", "OwnerWorkerId" },
                filter: "ReleasedAtUtc IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventSeatReservations_ChurchEventId_SpouseWorkerId",
                table: "EventSeatReservations",
                columns: new[] { "ChurchEventId", "SpouseWorkerId" },
                unique: true,
                filter: "SpouseWorkerId IS NOT NULL AND ReleasedAtUtc IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventSeatReservations_EventTableId_SeatNumber",
                table: "EventSeatReservations",
                columns: new[] { "EventTableId", "SeatNumber" },
                unique: true,
                filter: "ReleasedAtUtc IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BricksPrivilegeAudits");

            migrationBuilder.DropTable(
                name: "BricksUserPrivileges");

            migrationBuilder.DropTable(
                name: "EventSeatReservations");

            migrationBuilder.DropTable(
                name: "BricksPrivileges");
        }
    }
}
