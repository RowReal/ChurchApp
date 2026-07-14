using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileUpdateMultipleApprovers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProfileUpdateApprover",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProfileUpdateRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    ApproverWorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    HasActed = table.Column<bool>(type: "INTEGER", nullable: false),
                    Decision = table.Column<string>(type: "TEXT", nullable: true),
                    DecisionDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileUpdateApprover", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfileUpdateApprover_ProfileUpdateRequests_ProfileUpdateRequestId",
                        column: x => x.ProfileUpdateRequestId,
                        principalTable: "ProfileUpdateRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProfileUpdateApprover_Workers_ApproverWorkerId",
                        column: x => x.ApproverWorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfileUpdateApprover_ApproverWorkerId",
                table: "ProfileUpdateApprover",
                column: "ApproverWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileUpdateApprover_ProfileUpdateRequestId",
                table: "ProfileUpdateApprover",
                column: "ProfileUpdateRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfileUpdateApprover");
        }
    }
}
