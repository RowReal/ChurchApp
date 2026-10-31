using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddEventCheckInWindows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CheckInClosesAtUtc",
                table: "ChurchEvents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckInOpensAtUtc",
                table: "ChurchEvents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MenuChangesCloseAtUtc",
                table: "ChurchEvents",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckInClosesAtUtc",
                table: "ChurchEvents");

            migrationBuilder.DropColumn(
                name: "CheckInOpensAtUtc",
                table: "ChurchEvents");

            migrationBuilder.DropColumn(
                name: "MenuChangesCloseAtUtc",
                table: "ChurchEvents");
        }
    }
}
