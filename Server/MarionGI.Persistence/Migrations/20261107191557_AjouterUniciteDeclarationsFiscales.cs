using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarionGI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjouterUniciteDeclarationsFiscales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DeclarationsFiscales_Societe_Annee_Type",
                table: "DeclarationsFiscales");

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationsFiscales_Societe_Annee_Type",
                table: "DeclarationsFiscales",
                columns: new[] { "SocieteId", "AnneeFiscale", "TypeImpot" },
                unique: true,
                filter: "[EstSupprime] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DeclarationsFiscales_Societe_Annee_Type",
                table: "DeclarationsFiscales");

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationsFiscales_Societe_Annee_Type",
                table: "DeclarationsFiscales",
                columns: new[] { "SocieteId", "AnneeFiscale", "TypeImpot" });
        }
    }
}
