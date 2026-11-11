
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarionGI.Persistence.Migrations
{
    public partial class RendreMoisLoyerNullableEtCorrigerIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Supprimer les index dépendants de MoisLoyer.
            migrationBuilder.DropIndex(
                name: "IX_Paiements_ContratId_MoisLoyer",
                table: "Paiements");

            migrationBuilder.DropIndex(
                name: "IX_Paiements_ContratId_MoisLoyer_EnAttente",
                table: "Paiements");

            // Rendre MoisLoyer nullable.
            migrationBuilder.AlterColumn<DateTime>(
                name: "MoisLoyer",
                table: "Paiements",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime");

            // Restaurer l'index général.
            migrationBuilder.CreateIndex(
                name: "IX_Paiements_ContratId_MoisLoyer",
                table: "Paiements",
                columns: new[] { "ContratId", "MoisLoyer" });

            // Recréer l'index filtré, désormais non unique.
            migrationBuilder.CreateIndex(
                name: "IX_Paiements_ContratId_MoisLoyer_EnAttente",
                table: "Paiements",
                columns: new[] { "ContratId", "MoisLoyer" },
                filter: "[EstSupprime] = 0 AND [StatutTransaction] = 1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Supprimer les deux index.
            migrationBuilder.DropIndex(
                name: "IX_Paiements_ContratId_MoisLoyer",
                table: "Paiements");

            migrationBuilder.DropIndex(
                name: "IX_Paiements_ContratId_MoisLoyer_EnAttente",
                table: "Paiements");

            // Attention : cette opération nécessite qu'aucune ligne
            // ne possède MoisLoyer = NULL.
            migrationBuilder.AlterColumn<DateTime>(
                name: "MoisLoyer",
                table: "Paiements",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            // Restaurer les index d'origine.
            migrationBuilder.CreateIndex(
                name: "IX_Paiements_ContratId_MoisLoyer",
                table: "Paiements",
                columns: new[] { "ContratId", "MoisLoyer" });

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_ContratId_MoisLoyer_EnAttente",
                table: "Paiements",
                columns: new[] { "ContratId", "MoisLoyer" },
                unique: true,
                filter: "([EstSupprime]=(0) AND [StatutTransaction]=(1))");
        }
    }
}
