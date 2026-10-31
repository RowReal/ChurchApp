using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddEventSeatingConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventSeatingSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChurchEventId = table.Column<int>(type: "INTEGER", nullable: false),
                    MixDirectorates = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventSeatingSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventTables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChurchEventId = table.Column<int>(type: "INTEGER", nullable: false),
                    TableName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Capacity = table.Column<int>(type: "INTEGER", nullable: false),
                    IsLeadershipReserved = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTables", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventSeatingSettings");

            migrationBuilder.DropTable(
                name: "EventTables");
        }
    }
}
