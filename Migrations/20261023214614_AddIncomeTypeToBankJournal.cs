using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddIncomeTypeToBankJournal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IncomeTypeId",
                table: "BankAccountJournals",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountJournals_IncomeTypeId",
                table: "BankAccountJournals",
                column: "IncomeTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_BankAccountJournals_IncomeTypes_IncomeTypeId",
                table: "BankAccountJournals",
                column: "IncomeTypeId",
                principalTable: "IncomeTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankAccountJournals_IncomeTypes_IncomeTypeId",
                table: "BankAccountJournals");

            migrationBuilder.DropIndex(
                name: "IX_BankAccountJournals_IncomeTypeId",
                table: "BankAccountJournals");

            migrationBuilder.DropColumn(
                name: "IncomeTypeId",
                table: "BankAccountJournals");
        }
    }
}
