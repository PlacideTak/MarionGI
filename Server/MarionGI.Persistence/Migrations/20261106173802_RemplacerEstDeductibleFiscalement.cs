using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarionGI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemplacerEstDeductibleFiscalement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstDeductibleFiscalement",
                table: "ChargesBiens");

            migrationBuilder.AddColumn<decimal>(
                name: "AcomptesVerses",
                table: "DeclarationsFiscales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ChargesForfaitaires",
                table: "DeclarationsFiscales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ChargesReellesAdmissibles",
                table: "DeclarationsFiscales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ChargesReellesDeclarees",
                table: "DeclarationsFiscales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCalcul",
                table: "DeclarationsFiscales",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModeDeduction",
                table: "DeclarationsFiscales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OptionDeductionFiscaleId",
                table: "DeclarationsFiscales",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RevenuNetFoncier",
                table: "DeclarationsFiscales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SoldeImpot",
                table: "DeclarationsFiscales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalLoyersBruts",
                table: "DeclarationsFiscales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontantDeductibleFiscalement",
                table: "ChargesBiens",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatutDeductibilite",
                table: "ChargesBiens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DeclarationsChargesBiens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeclarationFiscaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChargeBienId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DescriptionCharge = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DateDepense = table.Column<DateTime>(type: "datetime", nullable: false),
                    MontantTTC = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StatutDeductibilite = table.Column<int>(type: "int", nullable: false),
                    MontantRetenu = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime", nullable: false),
                    EstSupprime = table.Column<bool>(type: "bit", nullable: false),
                    DateSuppression = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeclarationsChargesBiens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeclarationsChargesBiens_ChargesBiens_ChargeBienId",
                        column: x => x.ChargeBienId,
                        principalTable: "ChargesBiens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeclarationsChargesBiens_DeclarationsFiscales_DeclarationFiscaleId",
                        column: x => x.DeclarationFiscaleId,
                        principalTable: "DeclarationsFiscales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OptionsDeductionFiscales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SocieteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnneeFiscale = table.Column<int>(type: "int", nullable: false),
                    ModeDeduction = table.Column<int>(type: "int", nullable: false),
                    DateConfirmation = table.Column<DateTime>(type: "datetime", nullable: true),
                    EstVerrouillee = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DateVerrouillage = table.Column<DateTime>(type: "datetime", nullable: true),
                    Observations = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime", nullable: false),
                    EstSupprime = table.Column<bool>(type: "bit", nullable: false),
                    DateSuppression = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptionsDeductionFiscales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OptionsDeductionFiscales_Societes_SocieteId",
                        column: x => x.SocieteId,
                        principalTable: "Societes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationsFiscales_OptionDeductionFiscaleId",
                table: "DeclarationsFiscales",
                column: "OptionDeductionFiscaleId");

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationsChargesBiens_ChargeBienId",
                table: "DeclarationsChargesBiens",
                column: "ChargeBienId");

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationsChargesBiens_Declaration_Charge",
                table: "DeclarationsChargesBiens",
                columns: new[] { "DeclarationFiscaleId", "ChargeBienId" },
                unique: true,
                filter: "[EstSupprime] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OptionsDeductionFiscales_Societe_Annee",
                table: "OptionsDeductionFiscales",
                columns: new[] { "SocieteId", "AnneeFiscale" },
                unique: true,
                filter: "[EstSupprime] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_DeclarationsFiscales_OptionsDeductionFiscales_OptionDeductionFiscaleId",
                table: "DeclarationsFiscales",
                column: "OptionDeductionFiscaleId",
                principalTable: "OptionsDeductionFiscales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeclarationsFiscales_OptionsDeductionFiscales_OptionDeductionFiscaleId",
                table: "DeclarationsFiscales");

            migrationBuilder.DropTable(
                name: "DeclarationsChargesBiens");

            migrationBuilder.DropTable(
                name: "OptionsDeductionFiscales");

            migrationBuilder.DropIndex(
                name: "IX_DeclarationsFiscales_OptionDeductionFiscaleId",
                table: "DeclarationsFiscales");

            migrationBuilder.DropColumn(
                name: "AcomptesVerses",
                table: "DeclarationsFiscales");

            migrationBuilder.DropColumn(
                name: "ChargesForfaitaires",
                table: "DeclarationsFiscales");

            migrationBuilder.DropColumn(
                name: "ChargesReellesAdmissibles",
                table: "DeclarationsFiscales");

            migrationBuilder.DropColumn(
                name: "ChargesReellesDeclarees",
                table: "DeclarationsFiscales");

            migrationBuilder.DropColumn(
                name: "DateCalcul",
                table: "DeclarationsFiscales");

            migrationBuilder.DropColumn(
                name: "ModeDeduction",
                table: "DeclarationsFiscales");

            migrationBuilder.DropColumn(
                name: "OptionDeductionFiscaleId",
                table: "DeclarationsFiscales");

            migrationBuilder.DropColumn(
                name: "RevenuNetFoncier",
                table: "DeclarationsFiscales");

            migrationBuilder.DropColumn(
                name: "SoldeImpot",
                table: "DeclarationsFiscales");

            migrationBuilder.DropColumn(
                name: "TotalLoyersBruts",
                table: "DeclarationsFiscales");

            migrationBuilder.DropColumn(
                name: "MontantDeductibleFiscalement",
                table: "ChargesBiens");

            migrationBuilder.DropColumn(
                name: "StatutDeductibilite",
                table: "ChargesBiens");

            migrationBuilder.AddColumn<bool>(
                name: "EstDeductibleFiscalement",
                table: "ChargesBiens",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
