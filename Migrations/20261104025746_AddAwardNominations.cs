using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class AddAwardNominations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AwardProgrammes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AwardProgrammes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AwardCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AwardProgrammeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2500, nullable: false),
                    NominationAudience = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    NomineeType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    GenderFilter = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NominationAllowed = table.Column<bool>(type: "INTEGER", nullable: false),
                    RestrictNomineesToOwnDirectorate = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    OpensAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosesAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AwardCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AwardCategories_AwardProgrammes_AwardProgrammeId",
                        column: x => x.AwardProgrammeId,
                        principalTable: "AwardProgrammes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AwardNominations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AwardCategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    NominatorWorkerId = table.Column<int>(type: "INTEGER", nullable: false),
                    NomineeWorkerId = table.Column<int>(type: "INTEGER", nullable: true),
                    NomineeDirectorateId = table.Column<int>(type: "INTEGER", nullable: true),
                    NoNomination = table.Column<bool>(type: "INTEGER", nullable: false),
                    Justification = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    IsSubmitted = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AwardNominations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AwardNominations_AwardCategories_AwardCategoryId",
                        column: x => x.AwardCategoryId,
                        principalTable: "AwardCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AwardNominations_Directorates_NomineeDirectorateId",
                        column: x => x.NomineeDirectorateId,
                        principalTable: "Directorates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AwardNominations_Workers_NominatorWorkerId",
                        column: x => x.NominatorWorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AwardNominations_Workers_NomineeWorkerId",
                        column: x => x.NomineeWorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AwardCategories_AwardProgrammeId_Name",
                table: "AwardCategories",
                columns: new[] { "AwardProgrammeId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AwardNominations_AwardCategoryId_NominatorWorkerId",
                table: "AwardNominations",
                columns: new[] { "AwardCategoryId", "NominatorWorkerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AwardNominations_NominatorWorkerId",
                table: "AwardNominations",
                column: "NominatorWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_AwardNominations_NomineeDirectorateId",
                table: "AwardNominations",
                column: "NomineeDirectorateId");

            migrationBuilder.CreateIndex(
                name: "IX_AwardNominations_NomineeWorkerId",
                table: "AwardNominations",
                column: "NomineeWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_AwardProgrammes_Year_Name",
                table: "AwardProgrammes",
                columns: new[] { "Year", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AwardNominations");

            migrationBuilder.DropTable(
                name: "AwardCategories");

            migrationBuilder.DropTable(
                name: "AwardProgrammes");
        }
    }
}
