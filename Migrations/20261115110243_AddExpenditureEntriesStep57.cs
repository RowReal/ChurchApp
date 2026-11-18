using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenditureEntriesStep57 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceExpenditureId",
                table: "BankAccountJournals",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExpenditureRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExpenditureCategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    BankAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    Currency = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Payee = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PurposeDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    BankReference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Remarks = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ApprovalThresholdNgnSnapshot = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RequiresPastorApproval = table.Column<bool>(type: "INTEGER", nullable: false),
                    SubmittedByWorkerId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PostedJournalId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenditureRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenditureRecords_BankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenditureRecords_ExpenditureCategories_ExpenditureCategoryId",
                        column: x => x.ExpenditureCategoryId,
                        principalTable: "ExpenditureCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenditureApprovalActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExpenditureRecordId = table.Column<int>(type: "INTEGER", nullable: false),
                    Stage = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Decision = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    WorkerId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Comments = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ActionAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenditureApprovalActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenditureApprovalActions_ExpenditureRecords_ExpenditureRecordId",
                        column: x => x.ExpenditureRecordId,
                        principalTable: "ExpenditureRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenditureAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExpenditureRecordId = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenditureAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenditureAttachments_ExpenditureRecords_ExpenditureRecordId",
                        column: x => x.ExpenditureRecordId,
                        principalTable: "ExpenditureRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountJournals_SourceExpenditureId",
                table: "BankAccountJournals",
                column: "SourceExpenditureId",
                unique: true,
                filter: "SourceExpenditureId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenditureApprovalActions_ExpenditureRecordId_ActionAtUtc",
                table: "ExpenditureApprovalActions",
                columns: new[] { "ExpenditureRecordId", "ActionAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenditureAttachments_ExpenditureRecordId",
                table: "ExpenditureAttachments",
                column: "ExpenditureRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenditureAttachments_StoredFileName",
                table: "ExpenditureAttachments",
                column: "StoredFileName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenditureRecords_BankAccountId_BankReference",
                table: "ExpenditureRecords",
                columns: new[] { "BankAccountId", "BankReference" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenditureRecords_ExpenditureCategoryId",
                table: "ExpenditureRecords",
                column: "ExpenditureCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenditureApprovalActions");

            migrationBuilder.DropTable(
                name: "ExpenditureAttachments");

            migrationBuilder.DropTable(
                name: "ExpenditureRecords");

            migrationBuilder.DropIndex(
                name: "IX_BankAccountJournals_SourceExpenditureId",
                table: "BankAccountJournals");

            migrationBuilder.DropColumn(
                name: "SourceExpenditureId",
                table: "BankAccountJournals");
        }
    }
}
