using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSupervisoryClusters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupervisoryClusters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    HeadWorkerId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupervisoryClusters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupervisoryClusters_Workers_HeadWorkerId",
                        column: x => x.HeadWorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupervisoryClusterDirectorates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SupervisoryClusterId = table.Column<int>(type: "INTEGER", nullable: false),
                    DirectorateId = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupervisoryClusterDirectorates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupervisoryClusterDirectorates_Directorates_DirectorateId",
                        column: x => x.DirectorateId,
                        principalTable: "Directorates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupervisoryClusterDirectorates_SupervisoryClusters_SupervisoryClusterId",
                        column: x => x.SupervisoryClusterId,
                        principalTable: "SupervisoryClusters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupervisoryClusterDirectorates_DirectorateId",
                table: "SupervisoryClusterDirectorates",
                column: "DirectorateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupervisoryClusterDirectorates_SupervisoryClusterId_DirectorateId",
                table: "SupervisoryClusterDirectorates",
                columns: new[] { "SupervisoryClusterId", "DirectorateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupervisoryClusters_Code",
                table: "SupervisoryClusters",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupervisoryClusters_HeadWorkerId",
                table: "SupervisoryClusters",
                column: "HeadWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_SupervisoryClusters_Name",
                table: "SupervisoryClusters",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupervisoryClusterDirectorates");

            migrationBuilder.DropTable(
                name: "SupervisoryClusters");
        }
    }
}
