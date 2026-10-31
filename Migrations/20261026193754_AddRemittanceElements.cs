using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddRemittanceElements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RemittanceElementId",
                table: "RemittanceRules",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RemittanceElements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemittanceElements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RemittanceRules_RemittanceElementId",
                table: "RemittanceRules",
                column: "RemittanceElementId");

            migrationBuilder.AddForeignKey(
                name: "FK_RemittanceRules_RemittanceElements_RemittanceElementId",
                table: "RemittanceRules",
                column: "RemittanceElementId",
                principalTable: "RemittanceElements",
                principalColumn: "Id",
              onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RemittanceRules_RemittanceElements_RemittanceElementId",
                table: "RemittanceRules");

            migrationBuilder.DropTable(
                name: "RemittanceElements");

            migrationBuilder.DropIndex(
                name: "IX_RemittanceRules_RemittanceElementId",
                table: "RemittanceRules");

            migrationBuilder.DropColumn(
                name: "RemittanceElementId",
                table: "RemittanceRules");
        }
    }
}
