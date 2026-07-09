using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApprovalRequestTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRequestTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalWorkflowDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequestTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalWorkflowDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalWorkflowDefinitions_ApprovalRequestTypes_RequestTypeId",
                        column: x => x.RequestTypeId,
                        principalTable: "ApprovalRequestTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequestCode = table.Column<string>(type: "TEXT", nullable: false),
                    RequestTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkflowDefinitionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", nullable: false),
                    Details = table.Column<string>(type: "TEXT", nullable: false),
                    ApprovalSought = table.Column<string>(type: "TEXT", nullable: false),
                    RequestedByWorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    DirectorateId = table.Column<int>(type: "INTEGER", nullable: true),
                    DepartmentId = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentStepOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentApproverWorkerId = table.Column<int>(type: "INTEGER", nullable: true),
                    CurrentApproverType = table.Column<string>(type: "TEXT", nullable: true),
                    CurrentApproverRole = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalRequests_ApprovalRequestTypes_RequestTypeId",
                        column: x => x.RequestTypeId,
                        principalTable: "ApprovalRequestTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalRequests_ApprovalWorkflowDefinitions_WorkflowDefinitionId",
                        column: x => x.WorkflowDefinitionId,
                        principalTable: "ApprovalWorkflowDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalRequests_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalRequests_Directorates_DirectorateId",
                        column: x => x.DirectorateId,
                        principalTable: "Directorates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalRequests_Workers_CurrentApproverWorkerId",
                        column: x => x.CurrentApproverWorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalRequests_Workers_RequestedByWorkerId",
                        column: x => x.RequestedByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalWorkflowSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkflowDefinitionId = table.Column<int>(type: "INTEGER", nullable: false),
                    StepOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    StepName = table.Column<string>(type: "TEXT", nullable: false),
                    ApproverType = table.Column<string>(type: "TEXT", nullable: false),
                    ApproverRole = table.Column<string>(type: "TEXT", nullable: true),
                    ApproverPrivilegeCode = table.Column<string>(type: "TEXT", nullable: true),
                    SpecificApproverWorkerId = table.Column<int>(type: "INTEGER", nullable: true),
                    CanApprove = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanReject = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanRequestMoreInfo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanForward = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsFinalStep = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalWorkflowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalWorkflowSteps_ApprovalWorkflowDefinitions_WorkflowDefinitionId",
                        column: x => x.WorkflowDefinitionId,
                        principalTable: "ApprovalWorkflowDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalNotificationRecipients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApprovalRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecipientWorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    NotificationType = table.Column<string>(type: "TEXT", nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalNotificationRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalNotificationRecipients_ApprovalRequests_ApprovalRequestId",
                        column: x => x.ApprovalRequestId,
                        principalTable: "ApprovalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalNotificationRecipients_Workers_RecipientWorkerId",
                        column: x => x.RecipientWorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalRequestActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApprovalRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActionByWorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActionType = table.Column<string>(type: "TEXT", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: false),
                    FromStatus = table.Column<string>(type: "TEXT", nullable: true),
                    ToStatus = table.Column<string>(type: "TEXT", nullable: true),
                    FromStepOrder = table.Column<int>(type: "INTEGER", nullable: true),
                    ToStepOrder = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRequestActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalRequestActions_ApprovalRequests_ApprovalRequestId",
                        column: x => x.ApprovalRequestId,
                        principalTable: "ApprovalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalRequestActions_Workers_ActionByWorkerId",
                        column: x => x.ActionByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalRequestAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApprovalRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    FileType = table.Column<string>(type: "TEXT", nullable: true),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    UploadedByWorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRequestAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalRequestAttachments_ApprovalRequests_ApprovalRequestId",
                        column: x => x.ApprovalRequestId,
                        principalTable: "ApprovalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalRequestAttachments_Workers_UploadedByWorkerId",
                        column: x => x.UploadedByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalNotificationRecipients_ApprovalRequestId",
                table: "ApprovalNotificationRecipients",
                column: "ApprovalRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalNotificationRecipients_RecipientWorkerId",
                table: "ApprovalNotificationRecipients",
                column: "RecipientWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequestActions_ActionByWorkerId",
                table: "ApprovalRequestActions",
                column: "ActionByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequestActions_ApprovalRequestId",
                table: "ApprovalRequestActions",
                column: "ApprovalRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequestAttachments_ApprovalRequestId",
                table: "ApprovalRequestAttachments",
                column: "ApprovalRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequestAttachments_UploadedByWorkerId",
                table: "ApprovalRequestAttachments",
                column: "UploadedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_CurrentApproverWorkerId",
                table: "ApprovalRequests",
                column: "CurrentApproverWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_DepartmentId",
                table: "ApprovalRequests",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_DirectorateId",
                table: "ApprovalRequests",
                column: "DirectorateId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_RequestedByWorkerId",
                table: "ApprovalRequests",
                column: "RequestedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_RequestTypeId",
                table: "ApprovalRequests",
                column: "RequestTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_WorkflowDefinitionId",
                table: "ApprovalRequests",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequestTypes_Code",
                table: "ApprovalRequestTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowDefinitions_RequestTypeId",
                table: "ApprovalWorkflowDefinitions",
                column: "RequestTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowSteps_WorkflowDefinitionId",
                table: "ApprovalWorkflowSteps",
                column: "WorkflowDefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalNotificationRecipients");

            migrationBuilder.DropTable(
                name: "ApprovalRequestActions");

            migrationBuilder.DropTable(
                name: "ApprovalRequestAttachments");

            migrationBuilder.DropTable(
                name: "ApprovalWorkflowSteps");

            migrationBuilder.DropTable(
                name: "ApprovalRequests");

            migrationBuilder.DropTable(
                name: "ApprovalWorkflowDefinitions");

            migrationBuilder.DropTable(
                name: "ApprovalRequestTypes");
        }
    }
}
