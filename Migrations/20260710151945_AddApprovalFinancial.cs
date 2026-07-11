using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalFinancial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinancialRequestDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApprovalRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    AmountRequested = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    AmountApproved = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Purpose = table.Column<string>(type: "TEXT", nullable: true),
                    BudgetLine = table.Column<string>(type: "TEXT", nullable: true),
                    PaymentDetails = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialRequestDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialRequestDetails_ApprovalRequests_ApprovalRequestId",
                        column: x => x.ApprovalRequestId,
                        principalTable: "ApprovalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialRequestDetails_ApprovalRequestId",
                table: "FinancialRequestDetails",
                column: "ApprovalRequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinancialRequestDetails");
        }
    }
}
