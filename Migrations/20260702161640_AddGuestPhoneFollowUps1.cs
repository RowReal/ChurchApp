using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestPhoneFollowUps1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GuestPhoneFollowUp_Guests_GuestId",
                table: "GuestPhoneFollowUp");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GuestPhoneFollowUp",
                table: "GuestPhoneFollowUp");

            migrationBuilder.RenameTable(
                name: "GuestPhoneFollowUp",
                newName: "GuestPhoneFollowUps");

            migrationBuilder.RenameIndex(
                name: "IX_GuestPhoneFollowUp_GuestId",
                table: "GuestPhoneFollowUps",
                newName: "IX_GuestPhoneFollowUps_GuestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GuestPhoneFollowUps",
                table: "GuestPhoneFollowUps",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GuestPhoneFollowUps_Guests_GuestId",
                table: "GuestPhoneFollowUps",
                column: "GuestId",
                principalTable: "Guests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GuestPhoneFollowUps_Guests_GuestId",
                table: "GuestPhoneFollowUps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GuestPhoneFollowUps",
                table: "GuestPhoneFollowUps");

            migrationBuilder.RenameTable(
                name: "GuestPhoneFollowUps",
                newName: "GuestPhoneFollowUp");

            migrationBuilder.RenameIndex(
                name: "IX_GuestPhoneFollowUps_GuestId",
                table: "GuestPhoneFollowUp",
                newName: "IX_GuestPhoneFollowUp_GuestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GuestPhoneFollowUp",
                table: "GuestPhoneFollowUp",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GuestPhoneFollowUp_Guests_GuestId",
                table: "GuestPhoneFollowUp",
                column: "GuestId",
                principalTable: "Guests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
