using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class mydatabasenew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecordNominations_Workers_NominatorWorkerId",
                table: "RecordNominations");

            migrationBuilder.DropForeignKey(
                name: "FK_RecordNominations_Workers_NomineeWorkerId",
                table: "RecordNominations");

            migrationBuilder.DropIndex(
                name: "IX_RecordNominations_NominatorWorkerId",
                table: "RecordNominations");

            migrationBuilder.DropIndex(
                name: "IX_RecordNominations_NomineeWorkerId",
                table: "RecordNominations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RecordNominations_NominatorWorkerId",
                table: "RecordNominations",
                column: "NominatorWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_RecordNominations_NomineeWorkerId",
                table: "RecordNominations",
                column: "NomineeWorkerId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecordNominations_Workers_NominatorWorkerId",
                table: "RecordNominations",
                column: "NominatorWorkerId",
                principalTable: "Workers",
                principalColumn: "WorkerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RecordNominations_Workers_NomineeWorkerId",
                table: "RecordNominations",
                column: "NomineeWorkerId",
                principalTable: "Workers",
                principalColumn: "WorkerId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
