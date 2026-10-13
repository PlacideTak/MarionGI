
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarionGI.Persistence.Migrations
{
    public partial class AjouterBienImmobilierAuxTickets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "ValidationDevisRequise",
                table: "TicketsIncidents",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<Guid>(
                name: "UniteLocativeId",
                table: "TicketsIncidents",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "ResultatReparation",
                table: "TicketsIncidents",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reference",
                table: "TicketsIncidents",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<bool>(
                name: "DevisValide",
                table: "TicketsIncidents",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            // Ajouter d'abord la colonne comme nullable pour permettre
            // le remplissage des données existantes.
            migrationBuilder.AddColumn<Guid>(
                name: "BienImmobilierId",
                table: "TicketsIncidents",
                type: "uniqueidentifier",
                nullable: true);

            // Associer chaque ancien ticket à l'immeuble de son unité.
            migrationBuilder.Sql("""
                UPDATE ti
                SET ti.BienImmobilierId = ul.BienImmobilierId
                FROM TicketsIncidents AS ti
                INNER JOIN UnitesLocatives AS ul
                    ON ul.Id = ti.UniteLocativeId;
                """);

            // Éviter de poursuivre si certains tickets ne peuvent
            // pas être associés automatiquement à un immeuble.
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM TicketsIncidents
                    WHERE BienImmobilierId IS NULL
                )
                BEGIN
                    THROW 50001,
                        'Migration interrompue : certains tickets ne sont associes a aucun immeuble. Corrigez ces donnees puis relancez la migration.',
                        1;
                END;
                """);

            // Une fois les données renseignées, rendre la colonne obligatoire.
            migrationBuilder.AlterColumn<Guid>(
                name: "BienImmobilierId",
                table: "TicketsIncidents",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "StatutTransaction",
                table: "Paiements",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Statut",
                table: "Contrats",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Reference",
                table: "Contrats",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "ChargesBiens",
                columns: table => new
                {
                    Id = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false),
                    BienImmobilierId = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false),
                    UniteLocativeId = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: true),
                    DateDepense = table.Column<DateTime>(
                        type: "datetime",
                        nullable: false),
                    TypeCharge = table.Column<int>(
                        type: "int",
                        nullable: false),
                    Description = table.Column<string>(
                        type: "nvarchar(2000)",
                        maxLength: 2000,
                        nullable: false),
                    MontantTTC = table.Column<decimal>(
                        type: "decimal(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false),
                    MontantHT = table.Column<decimal>(
                        type: "decimal(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: true),
                    MontantTVA = table.Column<decimal>(
                        type: "decimal(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: true),
                    ReferenceFacture = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: true),
                    EstDeductibleFiscalement = table.Column<bool>(
                        type: "bit",
                        nullable: false,
                        defaultValue: false),
                    JustificationNonDeductibilite = table.Column<string>(
                        type: "nvarchar(1000)",
                        maxLength: 1000,
                        nullable: true),
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
                    table.PrimaryKey(
                        "PK_ChargesBiens",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_ChargesBiens_BiensImmobiliers_BienImmobilierId",
                        column: x => x.BienImmobilierId,
                        principalTable: "BiensImmobiliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);

                    table.ForeignKey(
                        name: "FK_ChargesBiens_UnitesLocatives_UniteLocativeId",
                        column: x => x.UniteLocativeId,
                        principalTable: "UnitesLocatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParametresFiscaux",
                columns: table => new
                {
                    Id = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false),
                    AnneeFiscale = table.Column<int>(
                        type: "int",
                        nullable: false),
                    CodeRegle = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false),
                    Description = table.Column<string>(
                        type: "nvarchar(1000)",
                        maxLength: 1000,
                        nullable: false),
                    Valeur = table.Column<decimal>(
                        type: "decimal(18,6)",
                        precision: 18,
                        scale: 6,
                        nullable: false),
                    TypeParametre = table.Column<int>(
                        type: "int",
                        nullable: false),
                    DateDebutValidite = table.Column<DateTime>(
                        type: "datetime",
                        nullable: false),
                    DateFinValidite = table.Column<DateTime>(
                        type: "datetime",
                        nullable: true),
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
                    table.PrimaryKey(
                        "PK_ParametresFiscaux",
                        x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegimesFiscaux",
                columns: table => new
                {
                    Id = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false),
                    Code = table.Column<string>(
                        type: "nvarchar(50)",
                        maxLength: 50,
                        nullable: false),
                    Libelle = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false),
                    Description = table.Column<string>(
                        type: "nvarchar(1000)",
                        maxLength: 1000,
                        nullable: false),
                    EstActif = table.Column<bool>(
                        type: "bit",
                        nullable: false,
                        defaultValue: true),
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
                    table.PrimaryKey(
                        "PK_RegimesFiscaux",
                        x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TranchesIRPP",
                columns: table => new
                {
                    Id = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false),
                    AnneeFiscale = table.Column<int>(
                        type: "int",
                        nullable: false),
                    SeuilMin = table.Column<decimal>(
                        type: "decimal(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false),
                    SeuilMax = table.Column<decimal>(
                        type: "decimal(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: true),
                    Taux = table.Column<decimal>(
                        type: "decimal(9,6)",
                        precision: 9,
                        scale: 6,
                        nullable: false),
                    OrdreTranche = table.Column<int>(
                        type: "int",
                        nullable: false),
                    Description = table.Column<string>(
                        type: "nvarchar(500)",
                        maxLength: 500,
                        nullable: true),
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
                    table.PrimaryKey(
                        "PK_TranchesIRPP",
                        x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketsIncidents_BienImmobilierId_Statut",
                table: "TicketsIncidents",
                columns: new[] { "BienImmobilierId", "Statut" });

            migrationBuilder.CreateIndex(
                name: "IX_Contrats_Reference",
                table: "Contrats",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChargesBiens_BienImmobilierId_DateDepense",
                table: "ChargesBiens",
                columns: new[] { "BienImmobilierId", "DateDepense" });

            migrationBuilder.CreateIndex(
                name: "IX_ChargesBiens_UniteLocativeId",
                table: "ChargesBiens",
                column: "UniteLocativeId");

            migrationBuilder.CreateIndex(
                name: "IX_ParametresFiscaux_AnneeFiscale_CodeRegle",
                table: "ParametresFiscaux",
                columns: new[] { "AnneeFiscale", "CodeRegle" },
                filter: "[EstSupprime] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RegimesFiscaux_Code",
                table: "RegimesFiscaux",
                column: "Code",
                unique: true,
                filter: "[EstSupprime] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TranchesIRPP_AnneeFiscale_OrdreTranche",
                table: "TranchesIRPP",
                columns: new[] { "AnneeFiscale", "OrdreTranche" },
                unique: true,
                filter: "[EstSupprime] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketsIncidents_BiensImmobiliers_BienImmobilierId",
                table: "TicketsIncidents",
                column: "BienImmobilierId",
                principalTable: "BiensImmobiliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketsIncidents_BiensImmobiliers_BienImmobilierId",
                table: "TicketsIncidents");

            migrationBuilder.DropTable(name: "ChargesBiens");
            migrationBuilder.DropTable(name: "ParametresFiscaux");
            migrationBuilder.DropTable(name: "RegimesFiscaux");
            migrationBuilder.DropTable(name: "TranchesIRPP");

            migrationBuilder.DropIndex(
                name: "IX_TicketsIncidents_BienImmobilierId_Statut",
                table: "TicketsIncidents");

            migrationBuilder.DropIndex(
                name: "IX_Contrats_Reference",
                table: "Contrats");

            migrationBuilder.DropColumn(
                name: "BienImmobilierId",
                table: "TicketsIncidents");

            migrationBuilder.AlterColumn<bool>(
                name: "ValidationDevisRequise",
                table: "TicketsIncidents",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "UniteLocativeId",
                table: "TicketsIncidents",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ResultatReparation",
                table: "TicketsIncidents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reference",
                table: "TicketsIncidents",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<bool>(
                name: "DevisValide",
                table: "TicketsIncidents",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "StatutTransaction",
                table: "Paiements",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<int>(
                name: "Statut",
                table: "Contrats",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Reference",
                table: "Contrats",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
