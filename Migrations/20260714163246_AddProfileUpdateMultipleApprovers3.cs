using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileUpdateMultipleApprovers3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProfileUpdateApprover_ProfileUpdateRequests_ProfileUpdateRequestId",
                table: "ProfileUpdateApprover");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfileUpdateApprover_Workers_ApproverWorkerId",
                table: "ProfileUpdateApprover");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProfileUpdateApprover",
                table: "ProfileUpdateApprover");

            migrationBuilder.RenameTable(
                name: "ProfileUpdateApprover",
                newName: "ProfileUpdateApprovers");

            migrationBuilder.RenameIndex(
                name: "IX_ProfileUpdateApprover_ProfileUpdateRequestId",
                table: "ProfileUpdateApprovers",
                newName: "IX_ProfileUpdateApprovers_ProfileUpdateRequestId");

            migrationBuilder.RenameIndex(
                name: "IX_ProfileUpdateApprover_ApproverWorkerId",
                table: "ProfileUpdateApprovers",
                newName: "IX_ProfileUpdateApprovers_ApproverWorkerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProfileUpdateApprovers",
                table: "ProfileUpdateApprovers",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProfileUpdateApprovers_ProfileUpdateRequests_ProfileUpdateRequestId",
                table: "ProfileUpdateApprovers",
                column: "ProfileUpdateRequestId",
                principalTable: "ProfileUpdateRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfileUpdateApprovers_Workers_ApproverWorkerId",
                table: "ProfileUpdateApprovers",
                column: "ApproverWorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProfileUpdateApprovers_ProfileUpdateRequests_ProfileUpdateRequestId",
                table: "ProfileUpdateApprovers");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfileUpdateApprovers_Workers_ApproverWorkerId",
                table: "ProfileUpdateApprovers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProfileUpdateApprovers",
                table: "ProfileUpdateApprovers");

            migrationBuilder.RenameTable(
                name: "ProfileUpdateApprovers",
                newName: "ProfileUpdateApprover");

            migrationBuilder.RenameIndex(
                name: "IX_ProfileUpdateApprovers_ProfileUpdateRequestId",
                table: "ProfileUpdateApprover",
                newName: "IX_ProfileUpdateApprover_ProfileUpdateRequestId");

            migrationBuilder.RenameIndex(
                name: "IX_ProfileUpdateApprovers_ApproverWorkerId",
                table: "ProfileUpdateApprover",
                newName: "IX_ProfileUpdateApprover_ApproverWorkerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProfileUpdateApprover",
                table: "ProfileUpdateApprover",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProfileUpdateApprover_ProfileUpdateRequests_ProfileUpdateRequestId",
                table: "ProfileUpdateApprover",
                column: "ProfileUpdateRequestId",
                principalTable: "ProfileUpdateRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfileUpdateApprover_Workers_ApproverWorkerId",
                table: "ProfileUpdateApprover",
                column: "ApproverWorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
