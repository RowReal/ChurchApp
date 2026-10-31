using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AllowCollectionRepostingAfterReversal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BankAccountJournals_SourceCollectionId",
                table: "BankAccountJournals");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountJournals_SourceCollectionId",
                table: "BankAccountJournals",
                column: "SourceCollectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BankAccountJournals_SourceCollectionId",
                table: "BankAccountJournals");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountJournals_SourceCollectionId",
                table: "BankAccountJournals",
                column: "SourceCollectionId",
                unique: true);
        }
    }
}
