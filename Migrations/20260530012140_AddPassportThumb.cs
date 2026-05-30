using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPassportThumb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PassportPhotoThumbPath",
                table: "Workers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PassportPhotoThumbPath",
                table: "Workers");
        }
    }
}
