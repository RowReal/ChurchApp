using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerAttendanceScoringRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttendanceFullScore",
                table: "Services",
                type: "INTEGER",
                nullable: false,
                defaultValue: 20);

            migrationBuilder.AddColumn<int>(
                name: "AttendanceIntermediateScore",
                table: "Services",
                type: "INTEGER",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "AttendanceLateScore",
                table: "Services",
                type: "INTEGER",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<decimal>(
                name: "AttendanceMonthlyWeight",
                table: "Services",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "LeaderFullScoreCutoff",
                table: "Services",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "LeaderIntermediateScoreCutoff",
                table: "Services",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseSeparateLeaderScoring",
                table: "Services",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "WorkerFullScoreCutoff",
                table: "Services",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "WorkerIntermediateScoreCutoff",
                table: "Services",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttendanceFullScore",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "AttendanceIntermediateScore",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "AttendanceLateScore",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "AttendanceMonthlyWeight",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "LeaderFullScoreCutoff",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "LeaderIntermediateScoreCutoff",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "UseSeparateLeaderScoring",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "WorkerFullScoreCutoff",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "WorkerIntermediateScoreCutoff",
                table: "Services");
        }
    }
}
