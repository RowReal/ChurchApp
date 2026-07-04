using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSecondaryDirectorateAndDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SecondaryDepartmentId",
                table: "Workers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecondaryDirectorateId",
                table: "Workers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workers_SecondaryDepartmentId",
                table: "Workers",
                column: "SecondaryDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_SecondaryDirectorateId",
                table: "Workers",
                column: "SecondaryDirectorateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Workers_Departments_SecondaryDepartmentId",
                table: "Workers",
                column: "SecondaryDepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Workers_Directorates_SecondaryDirectorateId",
                table: "Workers",
                column: "SecondaryDirectorateId",
                principalTable: "Directorates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workers_Departments_SecondaryDepartmentId",
                table: "Workers");

            migrationBuilder.DropForeignKey(
                name: "FK_Workers_Directorates_SecondaryDirectorateId",
                table: "Workers");

            migrationBuilder.DropIndex(
                name: "IX_Workers_SecondaryDepartmentId",
                table: "Workers");

            migrationBuilder.DropIndex(
                name: "IX_Workers_SecondaryDirectorateId",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "SecondaryDepartmentId",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "SecondaryDirectorateId",
                table: "Workers");
        }
    }
}
