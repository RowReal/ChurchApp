using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddChurchCollectionRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AvailableForServiceCollection",
                table: "IncomeTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ChurchCollectionRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    IncomeTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    CollectionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_ChurchCollectionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChurchCollectionRecords_IncomeTypes_IncomeTypeId",
                        column: x => x.IncomeTypeId,
                        principalTable: "IncomeTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChurchCollectionRecords_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChurchCollectionRecords_Workers_ApprovedByWorkerId",
                        column: x => x.ApprovedByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "WorkerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChurchCollectionRecords_Workers_RecordedByWorkerId",
                        column: x => x.RecordedByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "WorkerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChurchCollectionRecords_Workers_RemovedByWorkerId",
                        column: x => x.RemovedByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "WorkerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChurchCollectionRecords_Workers_ReturnedByWorkerId",
                        column: x => x.ReturnedByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "WorkerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChurchCollectionAmendments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CollectionRecordId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProposedServiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProposedIncomeTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProposedCollectionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_ChurchCollectionAmendments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChurchCollectionAmendments_ChurchCollectionRecords_CollectionRecordId",
                        column: x => x.CollectionRecordId,
                        principalTable: "ChurchCollectionRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChurchCollectionAmendments_IncomeTypes_ProposedIncomeTypeId",
                        column: x => x.ProposedIncomeTypeId,
                        principalTable: "IncomeTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChurchCollectionAmendments_Services_ProposedServiceId",
                        column: x => x.ProposedServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChurchCollectionAmendments_Workers_DecidedByWorkerId",
                        column: x => x.DecidedByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "WorkerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChurchCollectionAmendments_Workers_RequestedByWorkerId",
                        column: x => x.RequestedByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "WorkerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChurchCollectionAmendments_CollectionRecordId",
                table: "ChurchCollectionAmendments",
                column: "CollectionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchCollectionAmendments_DecidedByWorkerId",
                table: "ChurchCollectionAmendments",
                column: "DecidedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchCollectionAmendments_ProposedIncomeTypeId",
                table: "ChurchCollectionAmendments",
                column: "ProposedIncomeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchCollectionAmendments_ProposedServiceId",
                table: "ChurchCollectionAmendments",
                column: "ProposedServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchCollectionAmendments_RequestedByWorkerId",
                table: "ChurchCollectionAmendments",
                column: "RequestedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchCollectionAmendments_Status",
                table: "ChurchCollectionAmendments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchCollectionRecords_ApprovedByWorkerId",
                table: "ChurchCollectionRecords",
                column: "ApprovedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchCollectionRecords_CollectionDate",
                table: "ChurchCollectionRecords",
                column: "CollectionDate");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchCollectionRecords_IncomeTypeId",
                table: "ChurchCollectionRecords",
                column: "IncomeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchCollectionRecords_RecordedByWorkerId",
                table: "ChurchCollectionRecords",
                column: "RecordedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchCollectionRecords_RemovedByWorkerId",
                table: "ChurchCollectionRecords",
                column: "RemovedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchCollectionRecords_ReturnedByWorkerId",
                table: "ChurchCollectionRecords",
                column: "ReturnedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchCollectionRecords_ServiceId",
                table: "ChurchCollectionRecords",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ChurchCollectionRecords_Status",
                table: "ChurchCollectionRecords",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChurchCollectionAmendments");

            migrationBuilder.DropTable(
                name: "ChurchCollectionRecords");

            migrationBuilder.DropColumn(
                name: "AvailableForServiceCollection",
                table: "IncomeTypes");
        }
    }
}
