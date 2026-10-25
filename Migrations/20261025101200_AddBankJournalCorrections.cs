using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddBankJournalCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BankAccountJournals_ReversesJournalId",
                table: "BankAccountJournals");

            migrationBuilder.AddColumn<string>(
                name: "CorrectionKind",
                table: "BankAccountJournals",
                type: "TEXT",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RequestedCorrectAmount",
                table: "BankAccountJournals",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountJournals_ReversesJournalId",
                table: "BankAccountJournals",
                column: "ReversesJournalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BankAccountJournals_ReversesJournalId",
                table: "BankAccountJournals");

            migrationBuilder.DropColumn(
                name: "CorrectionKind",
                table: "BankAccountJournals");

            migrationBuilder.DropColumn(
                name: "RequestedCorrectAmount",
                table: "BankAccountJournals");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountJournals_ReversesJournalId",
                table: "BankAccountJournals",
                column: "ReversesJournalId",
                unique: true);
        }
    }
}
