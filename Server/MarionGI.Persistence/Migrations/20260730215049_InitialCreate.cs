using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarionGI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JournauxAudit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Entite = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntiteId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateAction = table.Column<DateTime>(type: "datetime", nullable: false),
                    DetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime", nullable: false),
                    EstSupprime = table.Column<bool>(type: "bit", nullable: false),
                    DateSuppression = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournauxAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Utilisateurs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Prenom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Telephone = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MotDePasseHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Statut = table.Column<bool>(type: "bit", nullable: false),
                    DerniereConnexion = table.Column<DateTime>(type: "datetime", nullable: true),
                    TelephoneVerifie = table.Column<bool>(type: "bit", nullable: false),
                    OtpSecret = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtpExpiration = table.Column<DateTime>(type: "datetime", nullable: true),
                    TentativesConnexionEchouees = table.Column<int>(type: "int", nullable: false),
                    VerrouilleJusquA = table.Column<DateTime>(type: "datetime", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime", nullable: false),
                    EstSupprime = table.Column<bool>(type: "bit", nullable: false),
                    DateSuppression = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilisateurs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Biens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Adresse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ville = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quartier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Superficie = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Loyer = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    ProprietaireId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Photos = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime", nullable: false),
                    EstSupprime = table.Column<bool>(type: "bit", nullable: false),
                    DateSuppression = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Biens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Biens_Utilisateurs_ProprietaireId",
                        column: x => x.ProprietaireId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lu = table.Column<bool>(type: "bit", nullable: false),
                    DateEnvoi = table.Column<DateTime>(type: "datetime", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime", nullable: false),
                    EstSupprime = table.Column<bool>(type: "bit", nullable: false),
                    DateSuppression = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateExpiration = table.Column<DateTime>(type: "datetime", nullable: false),
                    EstRevoque = table.Column<bool>(type: "bit", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime", nullable: false),
                    EstSupprime = table.Column<bool>(type: "bit", nullable: false),
                    DateSuppression = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Contrats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BienId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocataireId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateDebut = table.Column<DateTime>(type: "datetime", nullable: false),
                    DateFin = table.Column<DateTime>(type: "datetime", nullable: false),
                    MontantLoyer = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontantCaution = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime", nullable: false),
                    EstSupprime = table.Column<bool>(type: "bit", nullable: false),
                    DateSuppression = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contrats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contrats_Biens_BienId",
                        column: x => x.BienId,
                        principalTable: "Biens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contrats_Utilisateurs_LocataireId",
                        column: x => x.LocataireId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DemandesVisite",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BienId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NomProspect = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TelephoneProspect = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateSouhaitee = table.Column<DateTime>(type: "datetime", nullable: false),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime", nullable: false),
                    EstSupprime = table.Column<bool>(type: "bit", nullable: false),
                    DateSuppression = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandesVisite", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandesVisite_Biens_BienId",
                        column: x => x.BienId,
                        principalTable: "Biens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DemandesVisite_Utilisateurs_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Paiements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContratId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DatePaiement = table.Column<DateTime>(type: "datetime", nullable: false),
                    ModePaiement = table.Column<int>(type: "int", nullable: false),
                    StatutTransaction = table.Column<int>(type: "int", nullable: false),
                    ReferenceTransactionOperateur = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumeroQuittance = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime", nullable: false),
                    EstSupprime = table.Column<bool>(type: "bit", nullable: false),
                    DateSuppression = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paiements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Paiements_Contrats_ContratId",
                        column: x => x.ContratId,
                        principalTable: "Contrats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Biens_ProprietaireId",
                table: "Biens",
                column: "ProprietaireId");

            migrationBuilder.CreateIndex(
                name: "IX_Biens_Reference",
                table: "Biens",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contrats_BienId",
                table: "Contrats",
                column: "BienId");

            migrationBuilder.CreateIndex(
                name: "IX_Contrats_LocataireId",
                table: "Contrats",
                column: "LocataireId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandesVisite_AgentId",
                table: "DemandesVisite",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandesVisite_BienId",
                table: "DemandesVisite",
                column: "BienId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UtilisateurId",
                table: "Notifications",
                column: "UtilisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_ContratId",
                table: "Paiements",
                column: "ContratId");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_NumeroQuittance",
                table: "Paiements",
                column: "NumeroQuittance",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UtilisateurId",
                table: "RefreshTokens",
                column: "UtilisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_Email",
                table: "Utilisateurs",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_Telephone",
                table: "Utilisateurs",
                column: "Telephone",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemandesVisite");

            migrationBuilder.DropTable(
                name: "JournauxAudit");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Paiements");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Contrats");

            migrationBuilder.DropTable(
                name: "Biens");

            migrationBuilder.DropTable(
                name: "Utilisateurs");
        }
    }
}
