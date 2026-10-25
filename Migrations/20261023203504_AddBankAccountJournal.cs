using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddBankAccountJournal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankAccountJournals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BankAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Direction = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    TransactionType = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 25, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Narration = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    TransactionReference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SourceCollectionId = table.Column<int>(type: "INTEGER", nullable: true),
                    ChargeCategory = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RelatedTransactionReference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    InitiatedByWorkerId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    InitiatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PostedByWorkerId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PostedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ApprovedByWorkerId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DecidedByWorkerId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DecisionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DecisionComments = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ReversesJournalId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccountJournals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankAccountJournals_BankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankAccountJournals_ChurchCollectionRecords_SourceCollectionId",
                        column: x => x.SourceCollectionId,
                        principalTable: "ChurchCollectionRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountJournals_BankAccountId_TransactionDate",
                table: "BankAccountJournals",
                columns: new[] { "BankAccountId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountJournals_ReversesJournalId",
                table: "BankAccountJournals",
                column: "ReversesJournalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountJournals_SourceCollectionId",
                table: "BankAccountJournals",
                column: "SourceCollectionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountJournals_Status",
                table: "BankAccountJournals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountJournals_TransactionReference",
                table: "BankAccountJournals",
                column: "TransactionReference");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankAccountJournals");
        }
    }
}
