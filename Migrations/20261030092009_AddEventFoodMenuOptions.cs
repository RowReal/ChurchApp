using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddEventFoodMenuOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventMenuOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChurchEventId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventMenuOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventMenuOptions_ChurchEvents_ChurchEventId",
                        column: x => x.ChurchEventId,
                        principalTable: "ChurchEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventMenuOptions_ChurchEventId",
                table: "EventMenuOptions",
                column: "ChurchEventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventMenuOptions_ChurchEventId_Name",
                table: "EventMenuOptions",
                columns: new[] { "ChurchEventId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventMenuOptions");
        }
    }
}
