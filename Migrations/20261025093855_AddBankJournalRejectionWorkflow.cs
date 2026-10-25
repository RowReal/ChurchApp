using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddBankJournalRejectionWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResubmissionExplanation",
                table: "BankAccountJournals",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewHistory",
                table: "BankAccountJournals",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WithdrawalReason",
                table: "BankAccountJournals",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WithdrawnByWorkerId",
                table: "BankAccountJournals",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WithdrawnDate",
                table: "BankAccountJournals",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResubmissionExplanation",
                table: "BankAccountJournals");

            migrationBuilder.DropColumn(
                name: "ReviewHistory",
                table: "BankAccountJournals");

            migrationBuilder.DropColumn(
                name: "WithdrawalReason",
                table: "BankAccountJournals");

            migrationBuilder.DropColumn(
                name: "WithdrawnByWorkerId",
                table: "BankAccountJournals");

            migrationBuilder.DropColumn(
                name: "WithdrawnDate",
                table: "BankAccountJournals");
        }
    }
}
