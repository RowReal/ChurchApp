using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPrayerFocusManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrayerPoints",
                table: "PrayerFocuses");

            migrationBuilder.CreateTable(
                name: "PrayerPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PrayerFocusId = table.Column<int>(type: "INTEGER", nullable: false),
                    PointText = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrayerPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrayerPoints_PrayerFocuses_PrayerFocusId",
                        column: x => x.PrayerFocusId,
                        principalTable: "PrayerFocuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrayerPoints_PrayerFocusId",
                table: "PrayerPoints",
                column: "PrayerFocusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrayerPoints");

            migrationBuilder.AddColumn<string>(
                name: "PrayerPoints",
                table: "PrayerFocuses",
                type: "TEXT",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");
        }
    }
}
