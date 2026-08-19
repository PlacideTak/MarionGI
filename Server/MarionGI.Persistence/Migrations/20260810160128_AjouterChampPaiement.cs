using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarionGI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjouterChampPaiement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EnregistreParUtilisateurId",
                table: "Paiements",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnregistreParUtilisateurId",
                table: "Paiements");
        }
    }
}
