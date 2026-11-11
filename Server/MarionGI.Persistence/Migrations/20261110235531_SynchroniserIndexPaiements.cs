
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarionGI.Persistence.Migrations
{
    public partial class SynchroniserIndexPaiements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Aucun changement SQL nécessaire.
            // L'index IX_Paiements_ContratId existe déjà
            // dans la base de données.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Aucun changement SQL à annuler.
            // L'index existait avant cette migration.
        }
    }
}
