using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddBricksHouseFellowshipModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HouseFellowshipCentres",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    NearestLandmark = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    HostName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    HostPhone = table.Column<string>(type: "TEXT", maxLength: 25, nullable: false),
                    LeaderWorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    AssistantWorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    LeaderPhoneOverride = table.Column<string>(type: "TEXT", maxLength: 25, nullable: true),
                    StartedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByWorkerId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseFellowshipCentres", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseFellowshipCentres_Workers_AssistantWorkerId",
                        column: x => x.AssistantWorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseFellowshipCentres_Workers_LeaderWorkerId",
                        column: x => x.LeaderWorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HouseFellowshipClosures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FellowshipDate = table.Column<DateTime>(type: "date", nullable: false),
                    CentreId = table.Column<int>(type: "INTEGER", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ClosedByWorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsRevoked = table.Column<bool>(type: "INTEGER", nullable: false),
                    RevokedByWorkerId = table.Column<int>(type: "INTEGER", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseFellowshipClosures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseFellowshipClosures_HouseFellowshipCentres_CentreId",
                        column: x => x.CentreId,
                        principalTable: "HouseFellowshipCentres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseFellowshipClosures_Workers_ClosedByWorkerId",
                        column: x => x.ClosedByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HouseFellowshipLeadershipHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CentreId = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseFellowshipLeadershipHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseFellowshipLeadershipHistories_HouseFellowshipCentres_CentreId",
                        column: x => x.CentreId,
                        principalTable: "HouseFellowshipCentres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseFellowshipLeadershipHistories_Workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HouseFellowshipMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CentreId = table.Column<int>(type: "INTEGER", nullable: false),
                    Surname = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OtherNames = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 25, nullable: false),
                    WhatsAppNumber = table.Column<string>(type: "TEXT", maxLength: 25, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    Sex = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    AgeGroup = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    MaritalStatus = table.Column<string>(type: "TEXT", maxLength: 25, nullable: false),
                    IsChurchWorker = table.Column<bool>(type: "INTEGER", nullable: false),
                    DirectorateId = table.Column<int>(type: "INTEGER", nullable: true),
                    LinkedWorkerId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    JoinedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseFellowshipMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseFellowshipMembers_Directorates_DirectorateId",
                        column: x => x.DirectorateId,
                        principalTable: "Directorates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseFellowshipMembers_HouseFellowshipCentres_CentreId",
                        column: x => x.CentreId,
                        principalTable: "HouseFellowshipCentres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseFellowshipMembers_Workers_LinkedWorkerId",
                        column: x => x.LinkedWorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HouseFellowshipSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CentreId = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionDate = table.Column<DateTime>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SubmittedByWorkerId = table.Column<int>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseFellowshipSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseFellowshipSessions_HouseFellowshipCentres_CentreId",
                        column: x => x.CentreId,
                        principalTable: "HouseFellowshipCentres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HouseFellowshipMemberTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MemberId = table.Column<int>(type: "INTEGER", nullable: false),
                    FromCentreId = table.Column<int>(type: "INTEGER", nullable: false),
                    ToCentreId = table.Column<int>(type: "INTEGER", nullable: false),
                    TransferredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TransferredByWorkerId = table.Column<int>(type: "INTEGER", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseFellowshipMemberTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseFellowshipMemberTransfers_HouseFellowshipCentres_FromCentreId",
                        column: x => x.FromCentreId,
                        principalTable: "HouseFellowshipCentres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseFellowshipMemberTransfers_HouseFellowshipCentres_ToCentreId",
                        column: x => x.ToCentreId,
                        principalTable: "HouseFellowshipCentres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseFellowshipMemberTransfers_HouseFellowshipMembers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "HouseFellowshipMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HouseFellowshipAttendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    MemberId = table.Column<int>(type: "INTEGER", nullable: true),
                    VisitorName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    VisitorSex = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    VisitorAgeGroup = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    IsPresent = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseFellowshipAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseFellowshipAttendances_HouseFellowshipMembers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "HouseFellowshipMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseFellowshipAttendances_HouseFellowshipSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "HouseFellowshipSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HouseFellowshipOfferings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    RecordedByWorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseFellowshipOfferings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseFellowshipOfferings_HouseFellowshipSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "HouseFellowshipSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseFellowshipOfferings_Workers_RecordedByWorkerId",
                        column: x => x.RecordedByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HouseFellowshipAttendances_MemberId",
                table: "HouseFellowshipAttendances",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseFellowshipAttendances_SessionId_MemberId",
                table: "HouseFellowshipAttendances",
                columns: new[] { "SessionId", "MemberId" },
                unique: true,
                filter: "MemberId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HouseFellowshipCentres_AssistantWorkerId",
                table: "HouseFellowshipCentres",
                column: "AssistantWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseFellowshipCentres_Code",
                table: "HouseFellowshipCentres",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HouseFellowshipCentres_LeaderWorkerId",
                table: "HouseFellowshipCentres",
                column: "LeaderWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseFellowshipCentres_Name",
                table: "HouseFellowshipCentres",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HouseFellowshipClosures_CentreId",
                table: "HouseFellowshipClosures",
                column: "CentreId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseFellowshipClosures_ClosedByWorkerId",
                table: "HouseFellowshipClosures",
                column: "ClosedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseFellowshipClosures_FellowshipDate_CentreId",
                table: "HouseFellowshipClosures",
                columns: new[] { "FellowshipDate", "CentreId" });

            migrationBuilder.CreateIndex(
                name: "IX_HouseFellowshipLeadershipHistories_CentreId_Position_EffectiveFrom",
                table: "HouseFellowshipLeadershipHistories",
                columns: new[] { "CentreId", "Position", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_HouseFellowshipLeadershipHistories_WorkerId",
                table: "HouseFellowshipLeadershipHistories",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseFellowshipMembers_CentreId_IsActive",
                table: "HouseFellowshipMembers",
                columns: new[] { "CentreId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_HouseFellowshipMembers_DirectorateId",
                table: "HouseFellowshipMembers",
                column: "DirectorateId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseFellowshipMembers_LinkedWorkerId",
                table: "HouseFellowshipMembers",
                column: "LinkedWorkerId",
                unique: true,
                filter: "LinkedWorkerId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HouseFellowshipMemberTransfers_FromCentreId",
                table: "HouseFellowshipMemberTransfers",
                column: "FromCentreId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseFellowshipMemberTransfers_MemberId_TransferredAtUtc",
                table: "HouseFellowshipMemberTransfers",
                columns: new[] { "MemberId", "TransferredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_HouseFellowshipMemberTransfers_ToCentreId",
                table: "HouseFellowshipMemberTransfers",
                column: "ToCentreId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseFellowshipOfferings_RecordedByWorkerId",
                table: "HouseFellowshipOfferings",
                column: "RecordedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseFellowshipOfferings_SessionId",
                table: "HouseFellowshipOfferings",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HouseFellowshipSessions_CentreId_SessionDate",
                table: "HouseFellowshipSessions",
                columns: new[] { "CentreId", "SessionDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HouseFellowshipAttendances");

            migrationBuilder.DropTable(
                name: "HouseFellowshipClosures");

            migrationBuilder.DropTable(
                name: "HouseFellowshipLeadershipHistories");

            migrationBuilder.DropTable(
                name: "HouseFellowshipMemberTransfers");

            migrationBuilder.DropTable(
                name: "HouseFellowshipOfferings");

            migrationBuilder.DropTable(
                name: "HouseFellowshipMembers");

            migrationBuilder.DropTable(
                name: "HouseFellowshipSessions");

            migrationBuilder.DropTable(
                name: "HouseFellowshipCentres");
        }
    }
}
