using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestPhoneFollowUps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuestPhoneFollowUp",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuestId = table.Column<int>(type: "INTEGER", nullable: false),
                    CallDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CalledByWorkerId = table.Column<int>(type: "INTEGER", nullable: true),
                    CalledByName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    WasCallAnswered = table.Column<bool>(type: "INTEGER", nullable: false),
                    CallDurationMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    Outcome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PrayerRequest = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    WillGuestReturn = table.Column<bool>(type: "INTEGER", nullable: true),
                    NeedsVisitation = table.Column<bool>(type: "INTEGER", nullable: true),
                    WantsToJoinDepartment = table.Column<bool>(type: "INTEGER", nullable: true),
                    DepartmentInterest = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    WantsToMeetPastor = table.Column<bool>(type: "INTEGER", nullable: true),
                    NextFollowUpDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    GuestFeedback = table.Column<string>(type: "TEXT", maxLength: 1500, nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", maxLength: 1500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestPhoneFollowUp", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestPhoneFollowUp_Guests_GuestId",
                        column: x => x.GuestId,
                        principalTable: "Guests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuestPhoneFollowUp_GuestId",
                table: "GuestPhoneFollowUp",
                column: "GuestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestPhoneFollowUp");
        }
    }
}
