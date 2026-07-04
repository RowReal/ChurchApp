using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPrayerFocus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Title",
                table: "PrayerFocuses",
                newName: "WeekEndDate");

            migrationBuilder.RenameColumn(
                name: "FocusText",
                table: "PrayerFocuses",
                newName: "CreatedDate");

            migrationBuilder.AddColumn<string>(
                name: "BibleVerse",
                table: "PrayerFocuses",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrayerPoints",
                table: "PrayerFocuses",
                type: "TEXT",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Theme",
                table: "PrayerFocuses",
                type: "TEXT",
                maxLength: 150,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BibleVerse",
                table: "PrayerFocuses");

            migrationBuilder.DropColumn(
                name: "PrayerPoints",
                table: "PrayerFocuses");

            migrationBuilder.DropColumn(
                name: "Theme",
                table: "PrayerFocuses");

            migrationBuilder.RenameColumn(
                name: "WeekEndDate",
                table: "PrayerFocuses",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "PrayerFocuses",
                newName: "FocusText");
        }
    }
}
