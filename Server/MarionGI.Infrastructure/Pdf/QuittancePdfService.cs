using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using MarionGI.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MarionGI.Infrastructure.Pdf;

public class QuittancePdfService : IQuittancePdfService
{
    private readonly IWebHostEnvironment _env;

    public QuittancePdfService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public byte[] GenererQuittance(Paiement paiement)
    {
        // ============================================================
        // 1. Charger les paramètres MarionGI
        // ============================================================

        string webRootPath =
            _env.WebRootPath ??
            Path.Combine(_env.ContentRootPath, "wwwroot");

        string filePath = Path.Combine(
            webRootPath,
            "MarionGISettings.json");

        var config = new ParametresDto();

        if (File.Exists(filePath))
        {
            var jsonContent = File.ReadAllText(filePath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            config =
                JsonSerializer.Deserialize<ParametresDto>(
                    jsonContent,
                    options)
                ?? new ParametresDto();
        }

        // ============================================================
        // 2. Configuration QuestPDF
        // ============================================================

        QuestPDF.Settings.License = LicenseType.Community;

        // ============================================================
        // 3. Récupération sécurisée des informations
        // ============================================================

        var contrat = paiement.Contrat;

        var locataire = contrat?.Locataire;

        var uniteLocative = contrat?.UniteLocative;

        var bien = uniteLocative?.BienImmobilier;

        string nomLocataire =
            $"{locataire?.Prenom ?? string.Empty} {locataire?.Nom ?? string.Empty}"
            .Trim();

        if (string.IsNullOrWhiteSpace(nomLocataire))
        {
            nomLocataire = "Locataire";
        }

        string referenceBien =
            bien?.Reference ?? "N/A";

        string localisationBien =
            bien != null
                ? $"{bien.Quartier}, {bien.Ville}"
                : "N/A";

        string modePaiement =
            paiement.ModePaiement.ToString();

        string referenceTransaction =
            string.IsNullOrWhiteSpace(
                paiement.ReferenceTransactionOperateur)
                ? "N/A"
                : paiement.ReferenceTransactionOperateur;

        // ============================================================
        // 4. Génération du PDF
        // ============================================================

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5.Landscape());

                page.Margin(20);

                page.PageColor(Colors.White);

                page.DefaultTextStyle(
                    x => x
                        .FontSize(11)
                        .FontFamily("Arial"));

                // ====================================================
                // EN-TÊTE
                // ====================================================

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item()
                            .Text(config.NomSociete ?? "MarionGI")
                            .Bold()
                            .FontSize(18)
                            .FontColor("#1F6F54");

                        if (!string.IsNullOrWhiteSpace(config.Adresse))
                        {
                            col.Item()
                                .Text(config.Adresse)
                                .FontSize(8)
                                .NormalWeight();
                        }

                        if (!string.IsNullOrWhiteSpace(config.Telephone))
                        {
                            col.Item()
                                .Text(config.Telephone)
                                .FontSize(8)
                                .NormalWeight();
                        }

                        if (!string.IsNullOrWhiteSpace(config.Email))
                        {
                            col.Item()
                                .Text(config.Email)
                                .FontSize(8)
                                .NormalWeight();
                        }
                    });

                    row.ConstantItem(150).Column(col =>
                    {
                        col.Item()
                            .Text("QUITTANCE N°")
                            .Bold();

                        col.Item()
                            .Text(paiement.NumeroQuittance ?? "N/A")
                            .FontSize(12)
                            .FontColor("#1F6F54")
                            .Bold();
                    });
                });

                // ====================================================
                // CONTENU
                // ====================================================

                page.Content()
                    .PaddingVertical(15)
                    .Column(col =>
                    {
                        col.Item()
                            .LineHorizontal(1)
                            .LineColor("#1F6F54");

                        col.Item()
                            .PaddingTop(10)
                            .Text(
                                $"Reçu de M./Mme : {nomLocataire}")
                            .Bold();

                        col.Item()
                            .Text(
                                $"La somme de : {paiement.Montant:N0} FCFA")
                            .FontSize(14)
                            .Bold()
                            .FontColor("#1F6F54");

                        col.Item()
                            .Text(
                                $"Pour loyer de l'unité locative : {referenceBien} ({localisationBien})");

                        col.Item()
                            .Text(
                                $"Mode de règlement : {modePaiement}");

                        col.Item()
                            .Text(
                                $"Référence transaction : {referenceTransaction}");

                        col.Item()
                            .Text(
                                $"Date d'encaissement : {paiement.DatePaiement:dd/MM/yyyy HH:mm}");
                    });

                // ====================================================
                // PIED DE PAGE
                // ====================================================

                page.Footer().Row(row =>
                {
                    row.RelativeItem()
                        .Text(config.PiedDePage ?? string.Empty)
                        .FontSize(8)
                        .Italic();

                    row.ConstantItem(100)
                        .AlignRight()
                        .Text("Le Gestionnaire")
                        .Bold();
                });
            });
        });

        // ============================================================
        // 5. Générer le fichier PDF en mémoire
        // ============================================================

        return document.GeneratePdf();
    }
}