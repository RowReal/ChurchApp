using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalDecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApprovalDecisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApprovalRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkflowStepId = table.Column<int>(type: "INTEGER", nullable: false),
                    DecisionByWorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    DecisionType = table.Column<string>(type: "TEXT", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: false),
                    DecisionAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalDecisions_ApprovalRequests_ApprovalRequestId",
                        column: x => x.ApprovalRequestId,
                        principalTable: "ApprovalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalDecisions_ApprovalWorkflowSteps_WorkflowStepId",
                        column: x => x.WorkflowStepId,
                        principalTable: "ApprovalWorkflowSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalDecisions_Workers_DecisionByWorkerId",
                        column: x => x.DecisionByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalWorkflowConditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkflowDefinitionId = table.Column<int>(type: "INTEGER", nullable: false),
                    FieldName = table.Column<string>(type: "TEXT", nullable: false),
                    Operator = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    TargetStepOrder = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalWorkflowConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalWorkflowConditions_ApprovalWorkflowDefinitions_WorkflowDefinitionId",
                        column: x => x.WorkflowDefinitionId,
                        principalTable: "ApprovalWorkflowDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalWorkflowRecipients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkflowStepId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecipientType = table.Column<string>(type: "TEXT", nullable: false),
                    NotificationEvent = table.Column<string>(type: "TEXT", nullable: false),
                    SpecificWorkerId = table.Column<int>(type: "INTEGER", nullable: true),
                    RecipientRole = table.Column<string>(type: "TEXT", nullable: true),
                    RecipientPrivilegeCode = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalWorkflowRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalWorkflowRecipients_ApprovalWorkflowSteps_WorkflowStepId",
                        column: x => x.WorkflowStepId,
                        principalTable: "ApprovalWorkflowSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecisions_ApprovalRequestId",
                table: "ApprovalDecisions",
                column: "ApprovalRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecisions_DecisionByWorkerId",
                table: "ApprovalDecisions",
                column: "DecisionByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecisions_WorkflowStepId",
                table: "ApprovalDecisions",
                column: "WorkflowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowConditions_WorkflowDefinitionId",
                table: "ApprovalWorkflowConditions",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowRecipients_WorkflowStepId",
                table: "ApprovalWorkflowRecipients",
                column: "WorkflowStepId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalDecisions");

            migrationBuilder.DropTable(
                name: "ApprovalWorkflowConditions");

            migrationBuilder.DropTable(
                name: "ApprovalWorkflowRecipients");
        }
    }
}
