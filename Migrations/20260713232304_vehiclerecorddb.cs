using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class vehiclerecorddb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehicleRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NumberOfVehicles = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordedByWorkerId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleRecords_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleRecords_Workers_RecordedByWorkerId",
                        column: x => x.RecordedByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "WorkerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleRecords_RecordDate",
                table: "VehicleRecords",
                column: "RecordDate");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleRecords_RecordedByWorkerId",
                table: "VehicleRecords",
                column: "RecordedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleRecords_ServiceId_RecordDate",
                table: "VehicleRecords",
                columns: new[] { "ServiceId", "RecordDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleRecords");
        }
    }
}
