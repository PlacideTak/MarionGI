using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarionGI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutMoisLoyerPaiement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ====================================================
            // 1. Ajouter MoisLoyer temporairement nullable
            // ====================================================

            migrationBuilder.AddColumn<DateTime>(
                name: "MoisLoyer",
                table: "Paiements",
                type: "datetime",
                nullable: true);


            // ====================================================
            // 2. Initialiser les anciens paiements
            //
            // Exemple :
            // DatePaiement = 2026-10-07
            // MoisLoyer    = 2026-10-01
            // ====================================================

            migrationBuilder.Sql("""
                UPDATE Paiements
                SET MoisLoyer = DATEFROMPARTS(
                    YEAR(DatePaiement),
                    MONTH(DatePaiement),
                    1
                )
                WHERE MoisLoyer IS NULL;
                """);


            // ====================================================
            // 3. Rendre MoisLoyer obligatoire
            // ====================================================

            migrationBuilder.AlterColumn<DateTime>(
                name: "MoisLoyer",
                table: "Paiements",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);


            // ====================================================
            // 4. Index EN ATTENTE
            //
            // StatutTransaction = 1 => EnAttente
            //
            // Un seul paiement en attente pour :
            // Contrat + Mois
            //
            // Index créé directement en SQL car EF Core ne peut
            // pas représenter correctement deux index filtrés
            // ayant exactement les mêmes colonnes.
            // ====================================================

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX [IX_Paiements_ContratId_MoisLoyer_EnAttente]
                ON [Paiements] ([ContratId], [MoisLoyer])
                WHERE [EstSupprime] = 0
                  AND [StatutTransaction] = 1;
                """);


            // ====================================================
            // 5. Index CONFIRMÉ
            //
            // StatutTransaction = 2 => Confirme
            //
            // Celui-ci est géré par EF Core.
            // ====================================================

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_ContratId_MoisLoyer_Confirme",
                table: "Paiements",
                columns: new[]
                {
                    "ContratId",
                    "MoisLoyer"
                },
                unique: true,
                filter: "[EstSupprime] = 0 AND [StatutTransaction] = 2");
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ====================================================
            // Supprimer l'index EN ATTENTE créé manuellement
            // ====================================================

            migrationBuilder.Sql("""
                DROP INDEX [IX_Paiements_ContratId_MoisLoyer_EnAttente]
                ON [Paiements];
                """);


            // ====================================================
            // Supprimer l'index CONFIRMÉ
            // ====================================================

            migrationBuilder.DropIndex(
                name: "IX_Paiements_ContratId_MoisLoyer_Confirme",
                table: "Paiements");


            // ====================================================
            // Supprimer MoisLoyer
            // ====================================================

            migrationBuilder.DropColumn(
                name: "MoisLoyer",
                table: "Paiements");
        }
    }
}