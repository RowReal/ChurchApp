using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AwardMultipleNominationSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AwardNominations_AwardCategoryId_NominatorWorkerId",
                table: "AwardNominations");

            migrationBuilder.AddColumn<int>(
                name: "SlotNumber",
                table: "AwardNominations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "MaxNominationsPerWorker",
                table: "AwardCategories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "SelectionRule",
                table: "AwardCategories",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "Any");

            migrationBuilder.CreateIndex(
                name: "IX_AwardNominations_AwardCategoryId_NominatorWorkerId_SlotNumber",
                table: "AwardNominations",
                columns: new[] { "AwardCategoryId", "NominatorWorkerId", "SlotNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AwardNominations_AwardCategoryId_NominatorWorkerId_SlotNumber",
                table: "AwardNominations");

            migrationBuilder.DropColumn(
                name: "SlotNumber",
                table: "AwardNominations");

            migrationBuilder.DropColumn(
                name: "MaxNominationsPerWorker",
                table: "AwardCategories");

            migrationBuilder.DropColumn(
                name: "SelectionRule",
                table: "AwardCategories");

            migrationBuilder.CreateIndex(
                name: "IX_AwardNominations_AwardCategoryId_NominatorWorkerId",
                table: "AwardNominations",
                columns: new[] { "AwardCategoryId", "NominatorWorkerId" },
                unique: true);
        }
    }
}
