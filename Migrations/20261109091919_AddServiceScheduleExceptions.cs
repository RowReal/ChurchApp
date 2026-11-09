using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceScheduleExceptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceScheduleExceptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    OccurrenceDate = table.Column<DateTime>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsCancelled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CancelledByWorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    CancelledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RestoredByWorkerId = table.Column<int>(type: "INTEGER", nullable: true),
                    RestoredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceScheduleExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceScheduleExceptions_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceScheduleExceptions_Workers_CancelledByWorkerId",
                        column: x => x.CancelledByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceScheduleExceptions_Workers_RestoredByWorkerId",
                        column: x => x.RestoredByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceScheduleExceptions_CancelledByWorkerId",
                table: "ServiceScheduleExceptions",
                column: "CancelledByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceScheduleExceptions_RestoredByWorkerId",
                table: "ServiceScheduleExceptions",
                column: "RestoredByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceScheduleExceptions_ServiceId_OccurrenceDate",
                table: "ServiceScheduleExceptions",
                columns: new[] { "ServiceId", "OccurrenceDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceScheduleExceptions");
        }
    }
}
