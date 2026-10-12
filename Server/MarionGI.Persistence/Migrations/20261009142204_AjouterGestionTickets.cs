using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarionGI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjouterGestionTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketsIncidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniteLocativeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocataireId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Categorie = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    DescriptionDegats = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Priorite = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    Statut = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    DateResolution = table.Column<DateTime>(type: "datetime", nullable: true),
                    CoutEstime = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CoutTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    ImputableA = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ValidationDevisRequise = table.Column<bool>(type: "bit", nullable: false),
                    DevisValide = table.Column<bool>(type: "bit", nullable: false),
                    DateValidationDevis = table.Column<DateTime>(type: "datetime", nullable: true),
                    ValideParUtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime", nullable: false),
                    EstSupprime = table.Column<bool>(type: "bit", nullable: false),
                    DateSuppression = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketsIncidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketsIncidents_UnitesLocatives_UniteLocativeId",
                        column: x => x.UniteLocativeId,
                        principalTable: "UnitesLocatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketsIncidents_Utilisateurs_LocataireId",
                        column: x => x.LocataireId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketsIncidents_Utilisateurs_ValideParUtilisateurId",
                        column: x => x.ValideParUtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TicketsIntervenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketIncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomArtisan = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Specialite = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Telephone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MontantDevis = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MontantFacture = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DateAssignation = table.Column<DateTime>(type: "datetime", nullable: true),
                    DateIntervention = table.Column<DateTime>(type: "datetime", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime", nullable: false),
                    EstSupprime = table.Column<bool>(type: "bit", nullable: false),
                    DateSuppression = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketsIntervenants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketsIntervenants_TicketsIncidents_TicketIncidentId",
                        column: x => x.TicketIncidentId,
                        principalTable: "TicketsIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TicketsPhotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketIncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UrlPhoto = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    AjouteParUtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateAjout = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    DateCreation = table.Column<DateTime>(type: "datetime", nullable: false),
                    EstSupprime = table.Column<bool>(type: "bit", nullable: false),
                    DateSuppression = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketsPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketsPhotos_TicketsIncidents_TicketIncidentId",
                        column: x => x.TicketIncidentId,
                        principalTable: "TicketsIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketsPhotos_Utilisateurs_AjouteParUtilisateurId",
                        column: x => x.AjouteParUtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketsIncidents_LocataireId",
                table: "TicketsIncidents",
                column: "LocataireId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketsIncidents_UniteLocativeId_Statut",
                table: "TicketsIncidents",
                columns: new[] { "UniteLocativeId", "Statut" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketsIncidents_ValideParUtilisateurId",
                table: "TicketsIncidents",
                column: "ValideParUtilisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketsIntervenants_TicketIncidentId",
                table: "TicketsIntervenants",
                column: "TicketIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketsPhotos_AjouteParUtilisateurId",
                table: "TicketsPhotos",
                column: "AjouteParUtilisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketsPhotos_TicketIncidentId_DateAjout",
                table: "TicketsPhotos",
                columns: new[] { "TicketIncidentId", "DateAjout" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketsIntervenants");

            migrationBuilder.DropTable(
                name: "TicketsPhotos");

            migrationBuilder.DropTable(
                name: "TicketsIncidents");
        }
    }
}
