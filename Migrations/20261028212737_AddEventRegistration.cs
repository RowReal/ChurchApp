using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddEventRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChurchEventId = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkerId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    WillAttend = table.Column<bool>(type: "INTEGER", nullable: false),
                    RegisteredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AssistedByWorkerId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventRegistrations_ChurchEvents_ChurchEventId",
                        column: x => x.ChurchEventId,
                        principalTable: "ChurchEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventRegistrations_ChurchEventId_WillAttend",
                table: "EventRegistrations",
                columns: new[] { "ChurchEventId", "WillAttend" });

            migrationBuilder.CreateIndex(
                name: "IX_EventRegistrations_ChurchEventId_WorkerId",
                table: "EventRegistrations",
                columns: new[] { "ChurchEventId", "WorkerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventRegistrations");
        }
    }
}
