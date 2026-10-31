using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Migrations
{
    /// <inheritdoc />
    public partial class FixRemittanceRuleUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RemittanceRules_IncomeTypeId_EffectiveFrom",
                table: "RemittanceRules");

            migrationBuilder.CreateIndex(
                name: "IX_RemittanceRules_IncomeTypeId_RemittanceElementId_EffectiveFrom",
                table: "RemittanceRules",
                columns: new[] { "IncomeTypeId", "RemittanceElementId", "EffectiveFrom" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RemittanceRules_IncomeTypeId_RemittanceElementId_EffectiveFrom",
                table: "RemittanceRules");

            migrationBuilder.CreateIndex(
                name: "IX_RemittanceRules_IncomeTypeId_EffectiveFrom",
                table: "RemittanceRules",
                columns: new[] { "IncomeTypeId", "EffectiveFrom" },
                unique: true);
        }
    }
}
