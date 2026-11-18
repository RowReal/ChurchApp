using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenditureSetupStep56 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpenditureApprovalSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PastorApprovalThresholdNgn = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedByWorkerId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenditureApprovalSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpenditureCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RequiresPurposeDescription = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSystemOthers = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByWorkerId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedByWorkerId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenditureCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenditureCategories_IsSystemOthers",
                table: "ExpenditureCategories",
                column: "IsSystemOthers",
                unique: true,
                filter: "IsSystemOthers = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenditureCategories_Name",
                table: "ExpenditureCategories",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenditureApprovalSettings");

            migrationBuilder.DropTable(
                name: "ExpenditureCategories");
        }
    }
}
