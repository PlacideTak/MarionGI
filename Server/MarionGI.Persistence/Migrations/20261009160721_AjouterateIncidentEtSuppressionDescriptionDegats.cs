using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarionGI.Persistence.Migrations
{
    public partial class AjouterateIncidentEtSuppressionDescriptionDegats : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Supprimer l'ancienne colonne devenue inutile.
            migrationBuilder.DropColumn(
                name: "DescriptionDegats",
                table: "TicketsIncidents");

            // Ajouter temporairement la date comme colonne nullable.
            migrationBuilder.AddColumn<DateTime>(
                name: "DateIncident",
                table: "TicketsIncidents",
                type: "datetime",
                nullable: true);

            // Pour les tickets existants, utiliser la date de création
            // comme valeur initiale de la date de l'incident.
            migrationBuilder.Sql("""
                UPDATE [TicketsIncidents]
                SET [DateIncident] = [DateCreation]
                WHERE [DateIncident] IS NULL;
                """);

            // Rendre la colonne obligatoire après avoir rempli les anciennes lignes.
            migrationBuilder.AlterColumn<DateTime>(
                name: "DateIncident",
                table: "TicketsIncidents",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Supprimer la date ajoutée.
            migrationBuilder.DropColumn(
                name: "DateIncident",
                table: "TicketsIncidents");

            // Restaurer DescriptionDegats si la migration est annulée.
            migrationBuilder.AddColumn<string>(
                name: "DescriptionDegats",
                table: "TicketsIncidents",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }
    }
}