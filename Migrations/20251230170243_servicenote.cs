using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class servicenote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    ServiceDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ServiceType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ThemeOrSermonTitle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MinisterOrGuestSpeaker = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ServiceStartTime = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    ServiceEndTime = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    TechnicalIssues = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    OrderOfServiceChanges = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Disruptions = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    SafetyConcerns = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    NotableGuests = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    AttendancePattern = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SpecialParticipation = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ServiceFlow = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LeadershipAwareness = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    FollowUpNeeded = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    AdditionalNotes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    RecordedByWorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceNotes_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceNotes_Workers_RecordedByWorkerId",
                        column: x => x.RecordedByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceNotes_RecordedByWorkerId",
                table: "ServiceNotes",
                column: "RecordedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceNotes_ServiceId",
                table: "ServiceNotes",
                column: "ServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceNotes");
        }
    }
}
