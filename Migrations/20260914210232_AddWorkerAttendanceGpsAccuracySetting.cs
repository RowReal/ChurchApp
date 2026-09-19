using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerAttendanceGpsAccuracySetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "MaximumGpsAccuracyMetres",
                table: "WorkerAttendanceSettings",
                type: "REAL",
                nullable: false,
                defaultValue: 150.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaximumGpsAccuracyMetres",
                table: "WorkerAttendanceSettings");
        }
    }
}
