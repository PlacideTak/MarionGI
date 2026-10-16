using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarionGI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjouterValeursFiscalesEtDeclarations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Utilisateurs_SocieteId_Email",
                table: "Utilisateurs");

            migrationBuilder.DropIndex(
                name: "IX_Utilisateurs_SocieteId_Telephone",
                table: "Utilisateurs");

            migrationBuilder.DropIndex(
                name: "IX_UnitesLocatives_BienImmobilierId_Reference",
                table: "UnitesLocatives");

            migrationBuilder.DropIndex(
                name: "IX_ParametresFiscaux_AnneeFiscale_CodeRegle",
                table: "ParametresFiscaux");

            migrationBuilder.DropIndex(
                name: "IX_Paiements_ContratId_MoisLoyer_Confirme",
                table: "Paiements");

            migrationBuilder.DropIndex(
                name: "IX_Paiements_NumeroQuittance",
                table: "Paiements");

            migrationBuilder.DropIndex(
                name: "IX_DemandesVisite_UniteLocativeId",
                table: "DemandesVisite");

            migrationBuilder.DropIndex(
                name: "IX_Contrats_Reference",
                table: "Contrats");

            migrationBuilder.DropColumn(
                name: "Courriel",
                table: "TicketsIntervenants");

            migrationBuilder.DropColumn(
                name: "NomArtisan",
                table: "TicketsIntervenants");

            migrationBuilder.DropColumn(
                name: "Specialite",
                table: "TicketsIntervenants");

            migrationBuilder.DropColumn(
                name: "Telephone",
                table: "TicketsIntervenants");

            migrationBuilder.AlterColumn<int>(
                name: "TentativesConnexionEchouees",
                table: "Utilisateurs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "TelephoneVerifie",
                table: "Utilisateurs",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "Statut",
                table: "Utilisateurs",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateDerniereModificationProfil",
                table: "Utilisateurs",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ChargesLocativesMensuelles",
                table: "UnitesLocatives",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateDerniereRenovation",
                table: "UnitesLocatives",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DatePremiereMiseEnLocation",
                table: "UnitesLocatives",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EstExonereeFiscalement",
                table: "UnitesLocatives",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Etage",
                table: "UnitesLocatives",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontantCaution",
                table: "UnitesLocatives",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotifExoneration",
                table: "UnitesLocatives",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NombrePieces",
                table: "UnitesLocatives",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateDebutValidite",
                table: "TranchesIRPP",
                type: "datetime",
                nullable: false,
                defaultValue: new DateTime(1753, 1, 1));

            migrationBuilder.AddColumn<DateTime>(
                name: "DateDerniereVerification",
                table: "TranchesIRPP",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateFinValidite",
                table: "TranchesIRPP",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceLegale",
                table: "TranchesIRPP",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "TicketsPhotos",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomFichier",
                table: "TicketsPhotos",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateFinIntervention",
                table: "TicketsIntervenants",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observations",
                table: "TicketsIntervenants",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PrestataireId",
                table: "TicketsIntervenants",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ReferenceDevis",
                table: "TicketsIntervenants",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceFacture",
                table: "TicketsIntervenants",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "LocataireId",
                table: "TicketsIncidents",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "DeclarantId",
                table: "TicketsIncidents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "Actif",
                table: "Societes",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<string>(
                name: "ActivitePrincipale",
                table: "Societes",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdresseFiscale",
                table: "Societes",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CentreImpots",
                table: "Societes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Departement",
                table: "Societes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FormeJuridique",
                table: "Societes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroContribuable",
                table: "Societes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pays",
                table: "Societes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Prenom",
                table: "Societes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Societes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TypePersonne",
                table: "Societes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Libelle",
                table: "RegimesFiscaux",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "RegimesFiscaux",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateDebutValidite",
                table: "RegimesFiscaux",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateDerniereVerification",
                table: "RegimesFiscaux",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateFinValidite",
                table: "RegimesFiscaux",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceLegale",
                table: "RegimesFiscaux",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Valeur",
                table: "ParametresFiscaux",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldPrecision: 18,
                oldScale: 6);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ParametresFiscaux",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<string>(
                name: "ConditionsApplication",
                table: "ParametresFiscaux",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateDerniereVerification",
                table: "ParametresFiscaux",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceLegale",
                table: "ParametresFiscaux",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RegimeFiscalId",
                table: "ParametresFiscaux",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TypeImpot",
                table: "ParametresFiscaux",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateConfirmation",
                table: "Paiements",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FraisTransaction",
                table: "Paiements",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontantDu",
                table: "Paiements",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MotifEchec",
                table: "Paiements",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observations",
                table: "Paiements",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Statut",
                table: "DemandesVisite",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Observations",
                table: "DemandesVisite",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<string>(
                name: "CommentaireInterne",
                table: "DemandesVisite",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateConfirmee",
                table: "DemandesVisite",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateTraitement",
                table: "DemandesVisite",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailProspect",
                table: "DemandesVisite",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheminDocumentContrat",
                table: "Contrats",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateProchaineRevisionLoyer",
                table: "Contrats",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateSignature",
                table: "Contrats",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DelaiPreavisJours",
                table: "Contrats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontantChargesLocatives",
                table: "Contrats",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observations",
                table: "Contrats",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RenouvellementAutomatique",
                table: "Contrats",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RevisionLoyerAutorisee",
                table: "Contrats",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TauxRevisionLoyer",
                table: "Contrats",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheminJustificatif",
                table: "ChargesBiens",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateFacture",
                table: "ChargesBiens",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DatePaiement",
                table: "ChargesBiens",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateValidationFiscale",
                table: "ChargesBiens",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomFournisseur",
                table: "ChargesBiens",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroContribuableFournisseur",
                table: "ChargesBiens",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observations",
                table: "ChargesBiens",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AnneeConstruction",
                table: "BiensImmobiliers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Arrondissement",
                table: "BiensImmobiliers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateAcquisition",
                table: "BiensImmobiliers",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateDerniereEvaluation",
                table: "BiensImmobiliers",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Departement",
                table: "BiensImmobiliers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NombreEtages",
                table: "BiensImmobiliers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroLot",
                table: "BiensImmobiliers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroParcelle",
                table: "BiensImmobiliers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroTitreFoncier",
                table: "BiensImmobiliers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceCadastrale",
                table: "BiensImmobiliers",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "BiensImmobiliers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatutFoncier",
                table: "BiensImmobiliers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValeurAcquisition",
                table: "BiensImmobiliers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValeurEstimee",
                table: "BiensImmobiliers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeclarationsFiscales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SocieteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BienImmobilierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AnneeFiscale = table.Column<int>(type: "int", nullable: false),
                    TypeImpot = table.Column<int>(type: "int", nullable: false),
                    BaseImposable = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MontantImpot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DateEcheance = table.Column<DateTime>(type: "datetime", nullable: true),
                    DateDeclaration = table.Column<DateTime>(type: "datetime", nullable: true),
                    ReferenceDeclaration = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Observations = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime", nullable: false),
                    EstSupprime = table.Column<bool>(type: "bit", nullable: false),
                    DateSuppression = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeclarationsFiscales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeclarationsFiscales_BiensImmobiliers_BienImmobilierId",
                        column: x => x.BienImmobilierId,
                        principalTable: "BiensImmobiliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeclarationsFiscales_Societes_SocieteId",
                        column: x => x.SocieteId,
                        principalTable: "Societes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Prestataires",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TypePrestataire = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Nom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Prenom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NomEntreprise = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Specialite = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Telephone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Courriel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Adresse = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Ville = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NumeroContribuable = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Observations = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Actif = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SocieteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime", nullable: false),
                    EstSupprime = table.Column<bool>(type: "bit", nullable: false),
                    DateSuppression = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prestataires", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prestataires_Societes_SocieteId",
                        column: x => x.SocieteId,
                        principalTable: "Societes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ValeursFiscalesBiens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BienImmobilierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnneeFiscale = table.Column<int>(type: "int", nullable: false),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TypeValeur = table.Column<int>(type: "int", nullable: false),
                    DateEvaluation = table.Column<DateTime>(type: "datetime", nullable: false),
                    DateValidationOfficielle = table.Column<DateTime>(type: "datetime", nullable: true),
                    ReferenceJustificative = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CheminJustificatif = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Observations = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime", nullable: false),
                    EstSupprime = table.Column<bool>(type: "bit", nullable: false),
                    DateSuppression = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValeursFiscalesBiens", x => x.Id);
                    table.CheckConstraint("CK_ValeursFiscalesBiens_Dates", "[DateValidationOfficielle] IS NULL OR [DateValidationOfficielle] >= [DateEvaluation]");
                    table.CheckConstraint("CK_ValeursFiscalesBiens_Montant", "[Montant] > 0");
                    table.ForeignKey(
                        name: "FK_ValeursFiscalesBiens_BiensImmobiliers_BienImmobilierId",
                        column: x => x.BienImmobilierId,
                        principalTable: "BiensImmobiliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_SocieteId_Email",
                table: "Utilisateurs",
                columns: new[] { "SocieteId", "Email" },
                unique: true,
                filter: "[EstSupprime] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_SocieteId_Telephone",
                table: "Utilisateurs",
                columns: new[] { "SocieteId", "Telephone" },
                unique: true,
                filter: "[EstSupprime] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_UnitesLocatives_BienImmobilierId_Reference",
                table: "UnitesLocatives",
                columns: new[] { "BienImmobilierId", "Reference" },
                unique: true,
                filter: "[EstSupprime] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TicketsIntervenants_PrestataireId",
                table: "TicketsIntervenants",
                column: "PrestataireId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketsIntervenants_TicketIncidentId_PrestataireId",
                table: "TicketsIntervenants",
                columns: new[] { "TicketIncidentId", "PrestataireId" },
                unique: true,
                filter: "[EstSupprime] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TicketsIncidents_DeclarantId",
                table: "TicketsIncidents",
                column: "DeclarantId");

            migrationBuilder.CreateIndex(
                name: "IX_Societes_NumeroContribuable",
                table: "Societes",
                column: "NumeroContribuable");

            migrationBuilder.CreateIndex(
                name: "IX_ParametresFiscaux_Annee_Code_Regime",
                table: "ParametresFiscaux",
                columns: new[] { "AnneeFiscale", "CodeRegle", "RegimeFiscalId" },
                filter: "[EstSupprime] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ParametresFiscaux_RegimeFiscalId",
                table: "ParametresFiscaux",
                column: "RegimeFiscalId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ParametresFiscaux_DatesValidite",
                table: "ParametresFiscaux",
                sql: "[DateFinValidite] IS NULL OR [DateFinValidite] >= [DateDebutValidite]");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_ContratId_MoisLoyer",
                table: "Paiements",
                columns: new[] { "ContratId", "MoisLoyer" });

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_NumeroQuittance",
                table: "Paiements",
                column: "NumeroQuittance",
                unique: true,
                filter: "[EstSupprime] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_ReferenceTransactionOperateur",
                table: "Paiements",
                column: "ReferenceTransactionOperateur",
                unique: true,
                filter: "[EstSupprime] = 0 AND [ReferenceTransactionOperateur] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DemandesVisite_UniteLocativeId_DateSouhaitee",
                table: "DemandesVisite",
                columns: new[] { "UniteLocativeId", "DateSouhaitee" });

            migrationBuilder.CreateIndex(
                name: "IX_Contrats_Reference",
                table: "Contrats",
                column: "Reference",
                unique: true,
                filter: "[EstSupprime] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationsFiscales_BienImmobilierId",
                table: "DeclarationsFiscales",
                column: "BienImmobilierId");

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationsFiscales_Societe_Annee_Type",
                table: "DeclarationsFiscales",
                columns: new[] { "SocieteId", "AnneeFiscale", "TypeImpot" });

            migrationBuilder.CreateIndex(
                name: "IX_Prestataires_SocieteId_Nom",
                table: "Prestataires",
                columns: new[] { "SocieteId", "Nom" },
                filter: "[EstSupprime] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Prestataires_SocieteId_Specialite",
                table: "Prestataires",
                columns: new[] { "SocieteId", "Specialite" },
                filter: "[EstSupprime] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ValeursFiscalesBiens_Bien_Annee_Type",
                table: "ValeursFiscalesBiens",
                columns: new[] { "BienImmobilierId", "AnneeFiscale", "TypeValeur" });

            migrationBuilder.AddForeignKey(
                name: "FK_ParametresFiscaux_RegimesFiscaux_RegimeFiscalId",
                table: "ParametresFiscaux",
                column: "RegimeFiscalId",
                principalTable: "RegimesFiscaux",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketsIncidents_Utilisateurs_DeclarantId",
                table: "TicketsIncidents",
                column: "DeclarantId",
                principalTable: "Utilisateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketsIntervenants_Prestataires_PrestataireId",
                table: "TicketsIntervenants",
                column: "PrestataireId",
                principalTable: "Prestataires",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParametresFiscaux_RegimesFiscaux_RegimeFiscalId",
                table: "ParametresFiscaux");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketsIncidents_Utilisateurs_DeclarantId",
                table: "TicketsIncidents");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketsIntervenants_Prestataires_PrestataireId",
                table: "TicketsIntervenants");

            migrationBuilder.DropTable(
                name: "DeclarationsFiscales");

            migrationBuilder.DropTable(
                name: "Prestataires");

            migrationBuilder.DropTable(
                name: "ValeursFiscalesBiens");

            migrationBuilder.DropIndex(
                name: "IX_Utilisateurs_SocieteId_Email",
                table: "Utilisateurs");

            migrationBuilder.DropIndex(
                name: "IX_Utilisateurs_SocieteId_Telephone",
                table: "Utilisateurs");

            migrationBuilder.DropIndex(
                name: "IX_UnitesLocatives_BienImmobilierId_Reference",
                table: "UnitesLocatives");

            migrationBuilder.DropIndex(
                name: "IX_TicketsIntervenants_PrestataireId",
                table: "TicketsIntervenants");

            migrationBuilder.DropIndex(
                name: "IX_TicketsIntervenants_TicketIncidentId_PrestataireId",
                table: "TicketsIntervenants");

            migrationBuilder.DropIndex(
                name: "IX_TicketsIncidents_DeclarantId",
                table: "TicketsIncidents");

            migrationBuilder.DropIndex(
                name: "IX_Societes_NumeroContribuable",
                table: "Societes");

            migrationBuilder.DropIndex(
                name: "IX_ParametresFiscaux_Annee_Code_Regime",
                table: "ParametresFiscaux");

            migrationBuilder.DropIndex(
                name: "IX_ParametresFiscaux_RegimeFiscalId",
                table: "ParametresFiscaux");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ParametresFiscaux_DatesValidite",
                table: "ParametresFiscaux");

            migrationBuilder.DropIndex(
                name: "IX_Paiements_ContratId_MoisLoyer",
                table: "Paiements");

            migrationBuilder.DropIndex(
                name: "IX_Paiements_NumeroQuittance",
                table: "Paiements");

            migrationBuilder.DropIndex(
                name: "IX_Paiements_ReferenceTransactionOperateur",
                table: "Paiements");

            migrationBuilder.DropIndex(
                name: "IX_DemandesVisite_UniteLocativeId_DateSouhaitee",
                table: "DemandesVisite");

            migrationBuilder.DropIndex(
                name: "IX_Contrats_Reference",
                table: "Contrats");

            migrationBuilder.DropColumn(
                name: "DateDerniereModificationProfil",
                table: "Utilisateurs");

            migrationBuilder.DropColumn(
                name: "ChargesLocativesMensuelles",
                table: "UnitesLocatives");

            migrationBuilder.DropColumn(
                name: "DateDerniereRenovation",
                table: "UnitesLocatives");

            migrationBuilder.DropColumn(
                name: "DatePremiereMiseEnLocation",
                table: "UnitesLocatives");

            migrationBuilder.DropColumn(
                name: "EstExonereeFiscalement",
                table: "UnitesLocatives");

            migrationBuilder.DropColumn(
                name: "Etage",
                table: "UnitesLocatives");

            migrationBuilder.DropColumn(
                name: "MontantCaution",
                table: "UnitesLocatives");

            migrationBuilder.DropColumn(
                name: "MotifExoneration",
                table: "UnitesLocatives");

            migrationBuilder.DropColumn(
                name: "NombrePieces",
                table: "UnitesLocatives");

            migrationBuilder.DropColumn(
                name: "DateDebutValidite",
                table: "TranchesIRPP");

            migrationBuilder.DropColumn(
                name: "DateDerniereVerification",
                table: "TranchesIRPP");

            migrationBuilder.DropColumn(
                name: "DateFinValidite",
                table: "TranchesIRPP");

            migrationBuilder.DropColumn(
                name: "ReferenceLegale",
                table: "TranchesIRPP");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "TicketsPhotos");

            migrationBuilder.DropColumn(
                name: "NomFichier",
                table: "TicketsPhotos");

            migrationBuilder.DropColumn(
                name: "DateFinIntervention",
                table: "TicketsIntervenants");

            migrationBuilder.DropColumn(
                name: "Observations",
                table: "TicketsIntervenants");

            migrationBuilder.DropColumn(
                name: "PrestataireId",
                table: "TicketsIntervenants");

            migrationBuilder.DropColumn(
                name: "ReferenceDevis",
                table: "TicketsIntervenants");

            migrationBuilder.DropColumn(
                name: "ReferenceFacture",
                table: "TicketsIntervenants");

            migrationBuilder.DropColumn(
                name: "DeclarantId",
                table: "TicketsIncidents");

            migrationBuilder.DropColumn(
                name: "ActivitePrincipale",
                table: "Societes");

            migrationBuilder.DropColumn(
                name: "AdresseFiscale",
                table: "Societes");

            migrationBuilder.DropColumn(
                name: "CentreImpots",
                table: "Societes");

            migrationBuilder.DropColumn(
                name: "Departement",
                table: "Societes");

            migrationBuilder.DropColumn(
                name: "FormeJuridique",
                table: "Societes");

            migrationBuilder.DropColumn(
                name: "NumeroContribuable",
                table: "Societes");

            migrationBuilder.DropColumn(
                name: "Pays",
                table: "Societes");

            migrationBuilder.DropColumn(
                name: "Prenom",
                table: "Societes");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Societes");

            migrationBuilder.DropColumn(
                name: "TypePersonne",
                table: "Societes");

            migrationBuilder.DropColumn(
                name: "DateDebutValidite",
                table: "RegimesFiscaux");

            migrationBuilder.DropColumn(
                name: "DateDerniereVerification",
                table: "RegimesFiscaux");

            migrationBuilder.DropColumn(
                name: "DateFinValidite",
                table: "RegimesFiscaux");

            migrationBuilder.DropColumn(
                name: "ReferenceLegale",
                table: "RegimesFiscaux");

            migrationBuilder.DropColumn(
                name: "ConditionsApplication",
                table: "ParametresFiscaux");

            migrationBuilder.DropColumn(
                name: "DateDerniereVerification",
                table: "ParametresFiscaux");

            migrationBuilder.DropColumn(
                name: "ReferenceLegale",
                table: "ParametresFiscaux");

            migrationBuilder.DropColumn(
                name: "RegimeFiscalId",
                table: "ParametresFiscaux");

            migrationBuilder.DropColumn(
                name: "TypeImpot",
                table: "ParametresFiscaux");

            migrationBuilder.DropColumn(
                name: "DateConfirmation",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "FraisTransaction",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "MontantDu",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "MotifEchec",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "Observations",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "CommentaireInterne",
                table: "DemandesVisite");

            migrationBuilder.DropColumn(
                name: "DateConfirmee",
                table: "DemandesVisite");

            migrationBuilder.DropColumn(
                name: "DateTraitement",
                table: "DemandesVisite");

            migrationBuilder.DropColumn(
                name: "EmailProspect",
                table: "DemandesVisite");

            migrationBuilder.DropColumn(
                name: "CheminDocumentContrat",
                table: "Contrats");

            migrationBuilder.DropColumn(
                name: "DateProchaineRevisionLoyer",
                table: "Contrats");

            migrationBuilder.DropColumn(
                name: "DateSignature",
                table: "Contrats");

            migrationBuilder.DropColumn(
                name: "DelaiPreavisJours",
                table: "Contrats");

            migrationBuilder.DropColumn(
                name: "MontantChargesLocatives",
                table: "Contrats");

            migrationBuilder.DropColumn(
                name: "Observations",
                table: "Contrats");

            migrationBuilder.DropColumn(
                name: "RenouvellementAutomatique",
                table: "Contrats");

            migrationBuilder.DropColumn(
                name: "RevisionLoyerAutorisee",
                table: "Contrats");

            migrationBuilder.DropColumn(
                name: "TauxRevisionLoyer",
                table: "Contrats");

            migrationBuilder.DropColumn(
                name: "CheminJustificatif",
                table: "ChargesBiens");

            migrationBuilder.DropColumn(
                name: "DateFacture",
                table: "ChargesBiens");

            migrationBuilder.DropColumn(
                name: "DatePaiement",
                table: "ChargesBiens");

            migrationBuilder.DropColumn(
                name: "DateValidationFiscale",
                table: "ChargesBiens");

            migrationBuilder.DropColumn(
                name: "NomFournisseur",
                table: "ChargesBiens");

            migrationBuilder.DropColumn(
                name: "NumeroContribuableFournisseur",
                table: "ChargesBiens");

            migrationBuilder.DropColumn(
                name: "Observations",
                table: "ChargesBiens");

            migrationBuilder.DropColumn(
                name: "AnneeConstruction",
                table: "BiensImmobiliers");

            migrationBuilder.DropColumn(
                name: "Arrondissement",
                table: "BiensImmobiliers");

            migrationBuilder.DropColumn(
                name: "DateAcquisition",
                table: "BiensImmobiliers");

            migrationBuilder.DropColumn(
                name: "DateDerniereEvaluation",
                table: "BiensImmobiliers");

            migrationBuilder.DropColumn(
                name: "Departement",
                table: "BiensImmobiliers");

            migrationBuilder.DropColumn(
                name: "NombreEtages",
                table: "BiensImmobiliers");

            migrationBuilder.DropColumn(
                name: "NumeroLot",
                table: "BiensImmobiliers");

            migrationBuilder.DropColumn(
                name: "NumeroParcelle",
                table: "BiensImmobiliers");

            migrationBuilder.DropColumn(
                name: "NumeroTitreFoncier",
                table: "BiensImmobiliers");

            migrationBuilder.DropColumn(
                name: "ReferenceCadastrale",
                table: "BiensImmobiliers");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "BiensImmobiliers");

            migrationBuilder.DropColumn(
                name: "StatutFoncier",
                table: "BiensImmobiliers");

            migrationBuilder.DropColumn(
                name: "ValeurAcquisition",
                table: "BiensImmobiliers");

            migrationBuilder.DropColumn(
                name: "ValeurEstimee",
                table: "BiensImmobiliers");

            migrationBuilder.AlterColumn<int>(
                name: "TentativesConnexionEchouees",
                table: "Utilisateurs",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<bool>(
                name: "TelephoneVerifie",
                table: "Utilisateurs",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "Statut",
                table: "Utilisateurs",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Courriel",
                table: "TicketsIntervenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NomArtisan",
                table: "TicketsIntervenants",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Specialite",
                table: "TicketsIntervenants",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Telephone",
                table: "TicketsIntervenants",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "LocataireId",
                table: "TicketsIncidents",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "Actif",
                table: "Societes",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "Libelle",
                table: "RegimesFiscaux",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "RegimesFiscaux",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<decimal>(
                name: "Valeur",
                table: "ParametresFiscaux",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ParametresFiscaux",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<int>(
                name: "Statut",
                table: "DemandesVisite",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "Observations",
                table: "DemandesVisite",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

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
                name: "IX_UnitesLocatives_BienImmobilierId_Reference",
                table: "UnitesLocatives",
                columns: new[] { "BienImmobilierId", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParametresFiscaux_AnneeFiscale_CodeRegle",
                table: "ParametresFiscaux",
                columns: new[] { "AnneeFiscale", "CodeRegle" },
                filter: "[EstSupprime] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_ContratId_MoisLoyer_Confirme",
                table: "Paiements",
                columns: new[] { "ContratId", "MoisLoyer" },
                unique: true,
                filter: "[EstSupprime] = 0 AND [StatutTransaction] = 2");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_NumeroQuittance",
                table: "Paiements",
                column: "NumeroQuittance",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemandesVisite_UniteLocativeId",
                table: "DemandesVisite",
                column: "UniteLocativeId");

            migrationBuilder.CreateIndex(
                name: "IX_Contrats_Reference",
                table: "Contrats",
                column: "Reference",
                unique: true);
        }
    }
}
