using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddBroadcastMessaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BroadcastMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MessageBody = table.Column<string>(type: "TEXT", nullable: false),
                    SenderType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SenderDisplayLabel = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AudienceType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SentByWorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BroadcastMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BroadcastMessages_Workers_SentByWorkerId",
                        column: x => x.SentByWorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BroadcastAudienceDirectorates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BroadcastMessageId = table.Column<int>(type: "INTEGER", nullable: false),
                    DirectorateId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BroadcastAudienceDirectorates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BroadcastAudienceDirectorates_BroadcastMessages_BroadcastMessageId",
                        column: x => x.BroadcastMessageId,
                        principalTable: "BroadcastMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BroadcastAudienceDirectorates_Directorates_DirectorateId",
                        column: x => x.DirectorateId,
                        principalTable: "Directorates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BroadcastRecipients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BroadcastMessageId = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    FirstReadAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EmailNotificationSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailNotificationSentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EmailNotificationError = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BroadcastRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BroadcastRecipients_BroadcastMessages_BroadcastMessageId",
                        column: x => x.BroadcastMessageId,
                        principalTable: "BroadcastMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BroadcastRecipients_Workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BroadcastAudienceDirectorates_BroadcastMessageId_DirectorateId",
                table: "BroadcastAudienceDirectorates",
                columns: new[] { "BroadcastMessageId", "DirectorateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BroadcastAudienceDirectorates_DirectorateId",
                table: "BroadcastAudienceDirectorates",
                column: "DirectorateId");

            migrationBuilder.CreateIndex(
                name: "IX_BroadcastMessages_SentByWorkerId",
                table: "BroadcastMessages",
                column: "SentByWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_BroadcastRecipients_BroadcastMessageId_WorkerId",
                table: "BroadcastRecipients",
                columns: new[] { "BroadcastMessageId", "WorkerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BroadcastRecipients_WorkerId_IsRead",
                table: "BroadcastRecipients",
                columns: new[] { "WorkerId", "IsRead" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BroadcastAudienceDirectorates");

            migrationBuilder.DropTable(
                name: "BroadcastRecipients");

            migrationBuilder.DropTable(
                name: "BroadcastMessages");
        }
    }
}
