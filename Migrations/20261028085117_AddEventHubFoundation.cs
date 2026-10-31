using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddEventHubFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChurchEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Venue = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RegistrationOpensAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RegistrationClosesAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsPublished = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCancelled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByWorkerId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedByWorkerId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChurchEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChurchEvents_IsPublished_RegistrationOpensAtUtc_RegistrationClosesAtUtc",
                table: "ChurchEvents",
                columns: new[] { "IsPublished", "RegistrationOpensAtUtc", "RegistrationClosesAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ChurchEvents_StartsAtUtc",
                table: "ChurchEvents",
                column: "StartsAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChurchEvents");
        }
    }
}
