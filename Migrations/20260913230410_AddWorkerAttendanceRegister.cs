using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerAttendanceRegister : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttendanceCloseMinutesAfterStart",
                table: "Services",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AttendanceOpenMinutesBefore",
                table: "Services",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClockOutCloseMinutesAfterEnd",
                table: "Services",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "EnableWorkerAttendance",
                table: "Services",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "WorkerAttendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ServiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    AttendanceDate = table.Column<DateTime>(type: "date", nullable: false),
                    ClockInTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClockOutTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClockInLatitude = table.Column<double>(type: "REAL", nullable: true),
                    ClockInLongitude = table.Column<double>(type: "REAL", nullable: true),
                    ClockInDistanceMetres = table.Column<double>(type: "REAL", nullable: true),
                    ClockInAccuracyMetres = table.Column<double>(type: "REAL", nullable: true),
                    ClockOutLatitude = table.Column<double>(type: "REAL", nullable: true),
                    ClockOutLongitude = table.Column<double>(type: "REAL", nullable: true),
                    ClockOutDistanceMetres = table.Column<double>(type: "REAL", nullable: true),
                    ClockOutAccuracyMetres = table.Column<double>(type: "REAL", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkerAttendances_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkerAttendances_Workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerAttendances_AttendanceDate",
                table: "WorkerAttendances",
                column: "AttendanceDate");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerAttendances_ServiceId",
                table: "WorkerAttendances",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerAttendances_WorkerId",
                table: "WorkerAttendances",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerAttendances_WorkerId_ServiceId_AttendanceDate",
                table: "WorkerAttendances",
                columns: new[] { "WorkerId", "ServiceId", "AttendanceDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkerAttendances");

            migrationBuilder.DropColumn(
                name: "AttendanceCloseMinutesAfterStart",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "AttendanceOpenMinutesBefore",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "ClockOutCloseMinutesAfterEnd",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "EnableWorkerAttendance",
                table: "Services");
        }
    }
}
