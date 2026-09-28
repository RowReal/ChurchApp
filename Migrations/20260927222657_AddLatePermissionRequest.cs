using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddLatePermissionRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LatePermissionRequestDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApprovalRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    ServiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpectedArrivalTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LatePermissionRequestDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LatePermissionRequestDetails_ApprovalRequests_ApprovalRequestId",
                        column: x => x.ApprovalRequestId,
                        principalTable: "ApprovalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LatePermissionRequestDetails_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LatePermissionRequestDetails_ApprovalRequestId",
                table: "LatePermissionRequestDetails",
                column: "ApprovalRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LatePermissionRequestDetails_RequestedDate",
                table: "LatePermissionRequestDetails",
                column: "RequestedDate");

            migrationBuilder.CreateIndex(
                name: "IX_LatePermissionRequestDetails_ServiceId",
                table: "LatePermissionRequestDetails",
                column: "ServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LatePermissionRequestDetails");
        }
    }
}
