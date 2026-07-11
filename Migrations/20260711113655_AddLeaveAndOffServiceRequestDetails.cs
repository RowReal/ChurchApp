using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveAndOffServiceRequestDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeaveRequestDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApprovalRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RelieveOfficerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Purpose = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PendingAssignments = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    AssignmentHandler = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequestDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequestDetails_ApprovalRequests_ApprovalRequestId",
                        column: x => x.ApprovalRequestId,
                        principalTable: "ApprovalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeaveRequestDetails_Workers_RelieveOfficerId",
                        column: x => x.RelieveOfficerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OffServiceRequestDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApprovalRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsePredefinedService = table.Column<bool>(type: "INTEGER", nullable: false),
                    ServiceId = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomServiceName = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    RequestedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CustomServiceDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CustomServiceTime = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    NominatedBackupWorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OffServiceRequestDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OffServiceRequestDetails_ApprovalRequests_ApprovalRequestId",
                        column: x => x.ApprovalRequestId,
                        principalTable: "ApprovalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OffServiceRequestDetails_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OffServiceRequestDetails_Workers_NominatedBackupWorkerId",
                        column: x => x.NominatedBackupWorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestDetails_ApprovalRequestId",
                table: "LeaveRequestDetails",
                column: "ApprovalRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestDetails_RelieveOfficerId",
                table: "LeaveRequestDetails",
                column: "RelieveOfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_OffServiceRequestDetails_ApprovalRequestId",
                table: "OffServiceRequestDetails",
                column: "ApprovalRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OffServiceRequestDetails_NominatedBackupWorkerId",
                table: "OffServiceRequestDetails",
                column: "NominatedBackupWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_OffServiceRequestDetails_ServiceId",
                table: "OffServiceRequestDetails",
                column: "ServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaveRequestDetails");

            migrationBuilder.DropTable(
                name: "OffServiceRequestDetails");
        }
    }
}
