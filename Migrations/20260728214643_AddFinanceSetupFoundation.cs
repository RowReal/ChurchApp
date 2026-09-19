using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceSetupFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CurrencyCode",
                table: "BankAccounts",
                newName: "Currency");

            migrationBuilder.AddColumn<string>(
                name: "OpeningBalanceLockedBy",
                table: "BankAccounts",
                type: "TEXT",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OpeningBalanceLockedDate",
                table: "BankAccounts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IncomeCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncomeCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IncomeTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    IncomeCategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultBankAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    AccountSelectionMode = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    RemittanceApplicable = table.Column<bool>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncomeTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncomeTypes_BankAccounts_DefaultBankAccountId",
                        column: x => x.DefaultBankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IncomeTypes_IncomeCategories_IncomeCategoryId",
                        column: x => x.IncomeCategoryId,
                        principalTable: "IncomeCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RemittanceRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IncomeTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    CalculationBasis = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: false),
                    FixedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Remarks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemittanceRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RemittanceRules_IncomeTypes_IncomeTypeId",
                        column: x => x.IncomeTypeId,
                        principalTable: "IncomeTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_ShortName",
                table: "BankAccounts",
                column: "ShortName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncomeCategories_Code",
                table: "IncomeCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncomeCategories_Name",
                table: "IncomeCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncomeTypes_Code",
                table: "IncomeTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncomeTypes_DefaultBankAccountId",
                table: "IncomeTypes",
                column: "DefaultBankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomeTypes_IncomeCategoryId",
                table: "IncomeTypes",
                column: "IncomeCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomeTypes_Name",
                table: "IncomeTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RemittanceRules_IncomeTypeId_EffectiveFrom",
                table: "RemittanceRules",
                columns: new[] { "IncomeTypeId", "EffectiveFrom" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RemittanceRules");

            migrationBuilder.DropTable(
                name: "IncomeTypes");

            migrationBuilder.DropTable(
                name: "IncomeCategories");

            migrationBuilder.DropIndex(
                name: "IX_BankAccounts_ShortName",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "OpeningBalanceLockedBy",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "OpeningBalanceLockedDate",
                table: "BankAccounts");

            migrationBuilder.RenameColumn(
                name: "Currency",
                table: "BankAccounts",
                newName: "CurrencyCode");
        }
    }
}
