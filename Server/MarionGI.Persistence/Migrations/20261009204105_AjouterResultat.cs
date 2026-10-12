using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarionGI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjouterResultat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TicketsIncidents_Reference",
                table: "TicketsIncidents");

            migrationBuilder.AddColumn<string>(
                name: "Courriel",
                table: "TicketsIntervenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResultatReparation",
                table: "TicketsIncidents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketsIncidents_Reference",
                table: "TicketsIncidents",
                column: "Reference",
                unique: true,
                filter: "[EstSupprime] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TicketsIncidents_Reference",
                table: "TicketsIncidents");

            migrationBuilder.DropColumn(
                name: "Courriel",
                table: "TicketsIntervenants");

            migrationBuilder.DropColumn(
                name: "ResultatReparation",
                table: "TicketsIncidents");

            migrationBuilder.CreateIndex(
                name: "IX_TicketsIncidents_Reference",
                table: "TicketsIncidents",
                column: "Reference",
                unique: true);
        }
    }
}
