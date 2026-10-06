using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarionGI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePropertyUniteArchitecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // 1. Supprimer temporairement les anciennes FK
            // ============================================================

            migrationBuilder.DropForeignKey(
                name: "FK_Contrats_Biens_BienId",
                table: "Contrats");

            migrationBuilder.DropForeignKey(
                name: "FK_DemandesVisite_Biens_BienId",
                table: "DemandesVisite");

            migrationBuilder.DropForeignKey(
                name: "FK_Paiements_Contrats_ContratId",
                table: "Paiements");


            // ============================================================
            // 2. Supprimer les anciens index uniques utilisateurs
            // ============================================================

            migrationBuilder.DropIndex(
                name: "IX_Utilisateurs_Email",
                table: "Utilisateurs");

            migrationBuilder.DropIndex(
                name: "IX_Utilisateurs_Telephone",
                table: "Utilisateurs");


            // ============================================================
            // 3. Ajouter SocieteId temporairement nullable
            //    afin de pouvoir migrer les utilisateurs existants
            // ============================================================

            migrationBuilder.AddColumn<Guid>(
                name: "SocieteId",
                table: "Utilisateurs",
                type: "uniqueidentifier",
                nullable: true);


            // ============================================================
            // 4. Créer la table Societes
            // ============================================================

            migrationBuilder.CreateTable(
                name: "Societes",
                columns: table => new
                {
                    Id = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false),

                    Nom = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false),

                    NumeroEntreprise = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: true),

                    Adresse = table.Column<string>(
                        type: "nvarchar(250)",
                        maxLength: 250,
                        nullable: true),

                    Ville = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: true),

                    CodePostal = table.Column<string>(
                        type: "nvarchar(20)",
                        maxLength: 20,
                        nullable: true),

                    Telephone = table.Column<string>(
                        type: "nvarchar(30)",
                        maxLength: 30,
                        nullable: true),

                    Email = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true),

                    Actif = table.Column<bool>(
                        type: "bit",
                        nullable: false),

                    DateCreation = table.Column<DateTime>(
                        type: "datetime",
                        nullable: false),

                    EstSupprime = table.Column<bool>(
                        type: "bit",
                        nullable: false),

                    DateSuppression = table.Column<DateTime>(
                        type: "datetime",
                        nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Societes", x => x.Id);
                });


            // ============================================================
            // 5. Créer la société initiale
            // ============================================================

            migrationBuilder.Sql("""
        INSERT INTO Societes
        (
            Id,
            Nom,
            NumeroEntreprise,
            Adresse,
            Ville,
            CodePostal,
            Telephone,
            Email,
            Actif,
            DateCreation,
            EstSupprime,
            DateSuppression
        )
        VALUES
        (
            'D8A4C2E1-6F5B-4C9D-8A17-3E2F6B1C9045',
            'MarionGI',
            NULL,
            NULL,
            NULL,
            NULL,
            NULL,
            NULL,
            1,
            GETDATE(),
            0,
            NULL
        );
        """);


            // ============================================================
            // 6. Créer BienImmobiliers
            //    IMPORTANT : Biens existe encore à ce moment
            // ============================================================

            migrationBuilder.CreateTable(
                name: "BiensImmobiliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false),

                    Reference = table.Column<string>(
                        type: "nvarchar(50)",
                        maxLength: 50,
                        nullable: false),

                    Type = table.Column<int>(
                        type: "int",
                        nullable: false),

                    Adresse = table.Column<string>(
                        type: "nvarchar(250)",
                        maxLength: 250,
                        nullable: false),

                    Ville = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false),

                    Quartier = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false),

                    Superficie = table.Column<decimal>(
                        type: "decimal(10,2)",
                        precision: 10,
                        scale: 2,
                        nullable: false),

                    Statut = table.Column<int>(
                        type: "int",
                        nullable: false),

                    SocieteId = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false),

                    Photos = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    DateCreation = table.Column<DateTime>(
                        type: "datetime",
                        nullable: false),

                    EstSupprime = table.Column<bool>(
                        type: "bit",
                        nullable: false),

                    DateSuppression = table.Column<DateTime>(
                        type: "datetime",
                        nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BiensImmobiliers", x => x.Id);

                    table.ForeignKey(
                        name: "FK_BiensImmobiliers_Societes_SocieteId",
                        column: x => x.SocieteId,
                        principalTable: "Societes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });


            // ============================================================
            // 7. Créer les UnitesLocatives
            // ============================================================

            migrationBuilder.CreateTable(
                name: "UnitesLocatives",
                columns: table => new
                {
                    Id = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false),

                    Reference = table.Column<string>(
                        type: "nvarchar(50)",
                        maxLength: 50,
                        nullable: false),

                    Type = table.Column<int>(
                        type: "int",
                        nullable: false),

                    Superficie = table.Column<decimal>(
                        type: "decimal(10,2)",
                        precision: 10,
                        scale: 2,
                        nullable: false),

                    Loyer = table.Column<decimal>(
                        type: "decimal(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false),

                    Statut = table.Column<int>(
                        type: "int",
                        nullable: false),

                    BienImmobilierId = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false),

                    Photos = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    DateCreation = table.Column<DateTime>(
                        type: "datetime",
                        nullable: false),

                    EstSupprime = table.Column<bool>(
                        type: "bit",
                        nullable: false),

                    DateSuppression = table.Column<DateTime>(
                        type: "datetime",
                        nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitesLocatives", x => x.Id);

                    table.ForeignKey(
                        name: "FK_UnitesLocatives_BiensImmobiliers_BienImmobilierId",
                        column: x => x.BienImmobilierId,
                        principalTable: "BiensImmobiliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });


            // ============================================================
            // 8. Migrer les utilisateurs vers MarionGI
            // ============================================================

            migrationBuilder.Sql("""
        UPDATE Utilisateurs
        SET SocieteId = 'D8A4C2E1-6F5B-4C9D-8A17-3E2F6B1C9045';
        """);


            // ============================================================
            // 9. Migrer Biens -> BienImmobiliers
            // ============================================================

            migrationBuilder.Sql("""
        INSERT INTO BiensImmobiliers
        (
            Id,
            Reference,
            Type,
            Adresse,
            Ville,
            Quartier,
            Superficie,
            Statut,
            SocieteId,
            Photos,
            DateCreation,
            EstSupprime,
            DateSuppression
        )
        SELECT
            b.Id,
            b.Reference,
            b.Type,
            b.Adresse,
            b.Ville,
            b.Quartier,
            b.Superficie,
            b.Statut,
            'D8A4C2E1-6F5B-4C9D-8A17-3E2F6B1C9045',
            b.Photos,
            b.DateCreation,
            b.EstSupprime,
            b.DateSuppression
        FROM Biens b;
        """);


            // ============================================================
            // 10. Migrer chaque Bien -> une UniteLocative
            //
            // L'ancien Id du Bien devient l'Id de l'UniteLocative.
            // Cela permet de conserver les relations existantes.
            // ============================================================

            migrationBuilder.Sql("""
        INSERT INTO UnitesLocatives
        (
            Id,
            Reference,
            Type,
            Superficie,
            Loyer,
            Statut,
            BienImmobilierId,
            Photos,
            DateCreation,
            EstSupprime,
            DateSuppression
        )
        SELECT
            b.Id,
            b.Reference,
            b.Type,
            b.Superficie,
            b.Loyer,
            b.Statut,
            b.Id,
            b.Photos,
            b.DateCreation,
            b.EstSupprime,
            b.DateSuppression
        FROM Biens b;
        """);


            // ============================================================
            // 11. Transformer Contrats.BienId
            //     en Contrats.UniteLocativeId
            // ============================================================

            migrationBuilder.RenameColumn(
                name: "BienId",
                table: "Contrats",
                newName: "UniteLocativeId");

            migrationBuilder.RenameIndex(
                name: "IX_Contrats_BienId",
                table: "Contrats",
                newName: "IX_Contrats_UniteLocativeId");


            // ============================================================
            // 12. Transformer DemandesVisite.BienId
            //     en DemandesVisite.UniteLocativeId
            // ============================================================

            migrationBuilder.RenameColumn(
                name: "BienId",
                table: "DemandesVisite",
                newName: "UniteLocativeId");

            migrationBuilder.RenameIndex(
                name: "IX_DemandesVisite_BienId",
                table: "DemandesVisite",
                newName: "IX_DemandesVisite_UniteLocativeId");


            // ============================================================
            // 13. Ajouter Observations
            // ============================================================

            migrationBuilder.AddColumn<string>(
                name: "Observations",
                table: "DemandesVisite",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");


            // ============================================================
            // 14. Ajustements de longueurs
            // ============================================================

            migrationBuilder.AlterColumn<string>(
                name: "Telephone",
                table: "Utilisateurs",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "OtpSecret",
                table: "Utilisateurs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Utilisateurs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "RefreshTokens",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<bool>(
                name: "EstRevoque",
                table: "RefreshTokens",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceTransactionOperateur",
                table: "Paiements",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NumeroQuittance",
                table: "Paiements",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Notifications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "Notifications",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<bool>(
                name: "Lu",
                table: "Notifications",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "EntiteId",
                table: "JournauxAudit",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Entite",
                table: "JournauxAudit",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "JournauxAudit",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "TelephoneProspect",
                table: "DemandesVisite",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "NomProspect",
                table: "DemandesVisite",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");


            // ============================================================
            // 15. Index
            // ============================================================

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_SocieteId_Email",
                table: "Utilisateurs",
                columns: new[] { "SocieteId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_SocieteId_Telephone",
                table: "Utilisateurs",
                columns: new[] { "SocieteId", "Telephone" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BiensImmobiliers_SocieteId_Reference",
                table: "BiensImmobiliers",
                columns: new[] { "SocieteId", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitesLocatives_BienImmobilierId_Reference",
                table: "UnitesLocatives",
                columns: new[] { "BienImmobilierId", "Reference" },
                unique: true);


            // ============================================================
            // 16. Ajouter les nouvelles FK
            // ============================================================

            migrationBuilder.AddForeignKey(
                name: "FK_Contrats_UnitesLocatives_UniteLocativeId",
                table: "Contrats",
                column: "UniteLocativeId",
                principalTable: "UnitesLocatives",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DemandesVisite_UnitesLocatives_UniteLocativeId",
                table: "DemandesVisite",
                column: "UniteLocativeId",
                principalTable: "UnitesLocatives",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Paiements_Contrats_ContratId",
                table: "Paiements",
                column: "ContratId",
                principalTable: "Contrats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);


            // ============================================================
            // 17. Rendre SocieteId obligatoire maintenant que les données
            //     existantes ont été renseignées
            // ============================================================

            migrationBuilder.AlterColumn<Guid>(
                name: "SocieteId",
                table: "Utilisateurs",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);


            // ============================================================
            // 18. FK Utilisateur -> Societe
            // ============================================================

            migrationBuilder.AddForeignKey(
                name: "FK_Utilisateurs_Societes_SocieteId",
                table: "Utilisateurs",
                column: "SocieteId",
                principalTable: "Societes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);


            // ============================================================
            // 19. IMPORTANT :
            //     Maintenant seulement, on supprime l'ancienne table Biens
            // ============================================================

            migrationBuilder.DropTable(
                name: "Biens");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "Cette migration transforme les données de Biens vers BienImmobiliers et UnitesLocatives. " +
                "Le retour automatique vers l'ancien modèle n'est pas supporté afin d'éviter une perte de données.");
        }
    }
}
