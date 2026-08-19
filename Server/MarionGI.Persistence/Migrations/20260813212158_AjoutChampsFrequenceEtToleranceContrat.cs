using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarionGI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutChampsFrequenceEtToleranceContrat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DelaiJoursTolerance",
                table: "Contrats",
                type: "int",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<int>(
                name: "FrequencePaiement",
                table: "Contrats",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DelaiJoursTolerance",
                table: "Contrats");

            migrationBuilder.DropColumn(
                name: "FrequencePaiement",
                table: "Contrats");
        }
    }
}
