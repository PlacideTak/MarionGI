using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarionGI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdapterUniciteDeclarationsFiscalesParType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DeclarationsFiscales_BienImmobilierId",
                table: "DeclarationsFiscales");

            migrationBuilder.DropIndex(
                name: "IX_DeclarationsFiscales_Societe_Annee_Type",
                table: "DeclarationsFiscales");

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationsFiscales_ProprieteFonciere_Bien_Annee",
                table: "DeclarationsFiscales",
                columns: new[] { "BienImmobilierId", "AnneeFiscale" },
                unique: true,
                filter: "[EstSupprime] = 0 AND [TypeImpot] = 1 AND [BienImmobilierId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationsFiscales_RevenusFonciers_Societe_Annee",
                table: "DeclarationsFiscales",
                columns: new[] { "SocieteId", "AnneeFiscale" },
                unique: true,
                filter: "[EstSupprime] = 0 AND [TypeImpot] = 2 AND [BienImmobilierId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationsFiscales_Societe_Annee_Type",
                table: "DeclarationsFiscales",
                columns: new[] { "SocieteId", "AnneeFiscale", "TypeImpot" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DeclarationsFiscales_ProprieteFonciere_Bien_Annee",
                table: "DeclarationsFiscales");

            migrationBuilder.DropIndex(
                name: "IX_DeclarationsFiscales_RevenusFonciers_Societe_Annee",
                table: "DeclarationsFiscales");

            migrationBuilder.DropIndex(
                name: "IX_DeclarationsFiscales_Societe_Annee_Type",
                table: "DeclarationsFiscales");

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationsFiscales_BienImmobilierId",
                table: "DeclarationsFiscales",
                column: "BienImmobilierId");

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationsFiscales_Societe_Annee_Type",
                table: "DeclarationsFiscales",
                columns: new[] { "SocieteId", "AnneeFiscale", "TypeImpot" },
                unique: true,
                filter: "[EstSupprime] = 0");
        }
    }
}
