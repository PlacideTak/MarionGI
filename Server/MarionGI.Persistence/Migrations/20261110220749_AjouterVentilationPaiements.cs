using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarionGI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjouterVentilationPaiements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MontantChargesLocativesEncaisse",
                table: "Paiements",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontantLoyerEncaisse",
                table: "Paiements",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontantDeductibleFiscalement",
                table: "DeclarationsChargesBiens",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MontantChargesLocativesEncaisse",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "MontantLoyerEncaisse",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "MontantDeductibleFiscalement",
                table: "DeclarationsChargesBiens");
        }
    }
}
