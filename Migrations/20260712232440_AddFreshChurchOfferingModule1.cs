using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddFreshChurchOfferingModule1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChurchOfferingTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedByWorkerId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChurchOfferingTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChurchOfferingTypes_Workers_CreatedByWorkerId",
                        column: x => x.CreatedByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "WorkerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChurchOfferingRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    OfferingTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    OfferingDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    PaymentMode = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RecordedByWorkerId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ApprovedByWorkerId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReturnedByWorkerId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    ReturnedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReturnComment = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ResubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsRemoved = table.Column<bool>(type: "INTEGER", nullable: false),
                    RemovedByWorkerId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    RemovedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RemovalReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChurchOfferingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChurchOfferingRecords_ChurchOfferingTypes_OfferingTypeId",
                        column: x => x.OfferingTypeId,
                        principalTable: "ChurchOfferingTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChurchOfferingRecords_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChurchOfferingRecords_Workers_ApprovedByWorkerId",
                        column: x => x.ApprovedByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "WorkerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChurchOfferingRecords_Workers_RecordedByWorkerId",
                        column: x => x.RecordedByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "WorkerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChurchOfferingRecords_Workers_RemovedByWorkerId",
                        column: x => x.RemovedByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "WorkerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChurchOfferingRecords_Workers_ReturnedByWorkerId",
                        column: x => x.ReturnedByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "WorkerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChurchOfferingAmendments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OfferingRecordId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProposedServiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProposedOfferingTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProposedOfferingDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProposedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ProposedCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    ProposedPaymentMode = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ProposedRemarks = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    RequestedByWorkerId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    DecidedByWorkerId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DecisionComment = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChurchOfferingAmendments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChurchOfferingAmendments_ChurchOfferingRecords_OfferingRecordId",
                        column: x => x.OfferingRecordId,
                        principalTable: "ChurchOfferingRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChurchOfferingAmendments_ChurchOfferingTypes_ProposedOfferingTypeId",
                        column: x => x.ProposedOfferingTypeId,
                        principalTable: "ChurchOfferingTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChurchOfferingAmendments_Services_ProposedServiceId",
                        column: x => x.ProposedServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChurchOfferingAmendments_Workers_DecidedByWorkerId",
                        column: x => x.DecidedByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "WorkerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChurchOfferingAmendments_Workers_RequestedByWorkerId",
                        column: x => x.RequestedByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "WorkerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChurchOfferingAmendments_DecidedByWorkerId",
                table: "ChurchOfferingAmendments",
                column: "DecidedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchOfferingAmendments_OfferingRecordId",
                table: "ChurchOfferingAmendments",
                column: "OfferingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchOfferingAmendments_ProposedOfferingTypeId",
                table: "ChurchOfferingAmendments",
                column: "ProposedOfferingTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchOfferingAmendments_ProposedServiceId",
                table: "ChurchOfferingAmendments",
                column: "ProposedServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchOfferingAmendments_RequestedByWorkerId",
                table: "ChurchOfferingAmendments",
                column: "RequestedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchOfferingAmendments_Status",
                table: "ChurchOfferingAmendments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchOfferingRecords_ApprovedByWorkerId",
                table: "ChurchOfferingRecords",
                column: "ApprovedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchOfferingRecords_OfferingDate",
                table: "ChurchOfferingRecords",
                column: "OfferingDate");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchOfferingRecords_OfferingTypeId",
                table: "ChurchOfferingRecords",
                column: "OfferingTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchOfferingRecords_RecordedByWorkerId",
                table: "ChurchOfferingRecords",
                column: "RecordedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchOfferingRecords_RemovedByWorkerId",
                table: "ChurchOfferingRecords",
                column: "RemovedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchOfferingRecords_ReturnedByWorkerId",
                table: "ChurchOfferingRecords",
                column: "ReturnedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchOfferingRecords_ServiceId",
                table: "ChurchOfferingRecords",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchOfferingRecords_Status",
                table: "ChurchOfferingRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchOfferingTypes_CreatedByWorkerId",
                table: "ChurchOfferingTypes",
                column: "CreatedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchOfferingTypes_Name",
                table: "ChurchOfferingTypes",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChurchOfferingAmendments");

            migrationBuilder.DropTable(
                name: "ChurchOfferingRecords");

            migrationBuilder.DropTable(
                name: "ChurchOfferingTypes");
        }
    }
}
