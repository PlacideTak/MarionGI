using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarionGI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MiseAJourBienImmobilier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ====================================================
            // 1. Supprimer l'ancien index sur Reference
            // ====================================================

            migrationBuilder.DropIndex(
                name: "IX_BiensImmobiliers_SocieteId_Reference",
                table: "BiensImmobiliers");


            // ====================================================
            // 2. Supprimer l'ancien Statut du bien
            // ====================================================

            migrationBuilder.DropColumn(
                name: "Statut",
                table: "BiensImmobiliers");


            // ====================================================
            // 3. Ajouter Nom temporairement nullable
            // ====================================================

            migrationBuilder.AddColumn<string>(
                name: "Nom",
                table: "BiensImmobiliers",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);


            // ====================================================
            // 4. Donner un nom aux biens existants
            // ====================================================
            //
            // On utilise la référence existante afin de générer
            // automatiquement un nom unique dans la société.
            //
            // Exemple :
            // BIEN-001 -> Bien BIEN-001
            //
            migrationBuilder.Sql("""
                UPDATE BiensImmobiliers
                SET Nom = 'Bien ' + Reference
                WHERE Nom IS NULL;
            """);


            // ====================================================
            // 5. Rendre Nom obligatoire
            // ====================================================

            migrationBuilder.AlterColumn<string>(
                name: "Nom",
                table: "BiensImmobiliers",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150,
                oldNullable: true);


            // ====================================================
            // 6. Index unique sur SocieteId + Nom
            // ====================================================
            //
            // Un nom est unique dans une société.
            // Les biens supprimés sont exclus.
            //

            migrationBuilder.CreateIndex(
                name: "IX_BiensImmobiliers_SocieteId_Nom",
                table: "BiensImmobiliers",
                columns: new[] { "SocieteId", "Nom" },
                unique: true,
                filter: "[EstSupprime] = 0");


            // ====================================================
            // 7. Nouvel index unique sur Reference
            // ====================================================
            //
            // Les biens supprimés sont exclus.
            //

            migrationBuilder.CreateIndex(
                name: "IX_BiensImmobiliers_SocieteId_Reference",
                table: "BiensImmobiliers",
                columns: new[] { "SocieteId", "Reference" },
                unique: true,
                filter: "[EstSupprime] = 0");
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ====================================================
            // 1. Supprimer index Nom
            // ====================================================

            migrationBuilder.DropIndex(
                name: "IX_BiensImmobiliers_SocieteId_Nom",
                table: "BiensImmobiliers");


            // ====================================================
            // 2. Supprimer index Reference
            // ====================================================

            migrationBuilder.DropIndex(
                name: "IX_BiensImmobiliers_SocieteId_Reference",
                table: "BiensImmobiliers");


            // ====================================================
            // 3. Supprimer Nom
            // ====================================================

            migrationBuilder.DropColumn(
                name: "Nom",
                table: "BiensImmobiliers");


            // ====================================================
            // 4. Restaurer Statut
            // ====================================================

            migrationBuilder.AddColumn<int>(
                name: "Statut",
                table: "BiensImmobiliers",
                type: "int",
                nullable: false,
                defaultValue: 0);


            // ====================================================
            // 5. Restaurer l'ancien index Reference
            // ====================================================

            migrationBuilder.CreateIndex(
                name: "IX_BiensImmobiliers_SocieteId_Reference",
                table: "BiensImmobiliers",
                columns: new[] { "SocieteId", "Reference" },
                unique: true);
        }
    }
}