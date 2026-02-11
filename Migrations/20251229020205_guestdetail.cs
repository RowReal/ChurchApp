using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class guestdetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Guests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuestNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RecordingDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MiddleName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Surname = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Sex = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    AgeGroup = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Landmark = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    WhatsAppNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsRCCGMember = table.Column<bool>(type: "INTEGER", nullable: false),
                    OtherChurch = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    HowFoundUs = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    InvitedByName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ServiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    VisitingDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RecordedByWorkerId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RecordedByName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsSecondTimer = table.Column<bool>(type: "INTEGER", nullable: false),
                    SecondVisitDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SecondVisitRecordedByWorkerId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SecondVisitRecordedByName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CurrentPhoneNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    WantsToBecomeMember = table.Column<bool>(type: "INTEGER", nullable: true),
                    BirthMonth = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsBaptisedByWater = table.Column<bool>(type: "INTEGER", nullable: true),
                    WantsToJoinWorkforce = table.Column<bool>(type: "INTEGER", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Guests_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Guests_Workers_RecordedByWorkerId",
                        column: x => x.RecordedByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "WorkerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Guests_Email",
                table: "Guests",
                column: "Email",
                filter: "[Email] IS NOT NULL AND [Email] != ''");

            migrationBuilder.CreateIndex(
                name: "IX_Guests_GuestNumber",
                table: "Guests",
                column: "GuestNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Guests_IsSecondTimer_IsActive",
                table: "Guests",
                columns: new[] { "IsSecondTimer", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Guests_PhoneNumber",
                table: "Guests",
                column: "PhoneNumber",
                filter: "[PhoneNumber] IS NOT NULL AND [PhoneNumber] != ''");

            migrationBuilder.CreateIndex(
                name: "IX_Guests_RecordedByWorkerId",
                table: "Guests",
                column: "RecordedByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_Guests_RecordingDate",
                table: "Guests",
                column: "RecordingDate");

            migrationBuilder.CreateIndex(
                name: "IX_Guests_ServiceId",
                table: "Guests",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Guests_VisitingDate",
                table: "Guests",
                column: "VisitingDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Guests");
        }
    }
}
