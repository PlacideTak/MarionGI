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
        // 1. Charger les paramètres depuis wwwroot/pdfSettings.json
        string webRootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        string filePath = Path.Combine(webRootPath, "MarionGISettings.json");

        var config = new ParametresDto();
        if (File.Exists(filePath))
        {
            var jsonContent = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            config = JsonSerializer.Deserialize<ParametresDto>(jsonContent, options) ?? new ParametresDto();
        }

        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5.Landscape());
                page.Margin(20);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        // UTILISATION DES PARAMÈTRES ICI
                        col.Item().Text(config.NomSociete).Bold().FontSize(18).FontColor("#1F6F54");
                        col.Item().Text(config.Adresse).FontSize(8).NormalWeight();
                        col.Item().Text(config.Telephone).FontSize(8).NormalWeight();
                        col.Item().Text(config.Email).FontSize(8).NormalWeight();
                    });

                    row.ConstantItem(150).Column(col =>
                    {
                        col.Item().Text($"QUITTANCE N°").Bold();
                        col.Item().Text(paiement.NumeroQuittance).FontSize(12).FontColor("#1F6F54").Bold();
                    });
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Item().LineHorizontal(1).LineColor("#1F6F54");
                    col.Item().PaddingTop(10).Text($"Reçu de M./Mme : {paiement.Contrat.Locataire?.Prenom} {paiement.Contrat.Locataire?.Nom}").Bold();
                    col.Item().Text($"La somme de : {paiement.Montant:N0} FCFA").FontSize(14).Bold().FontColor("#1F6F54");
                    col.Item().Text($"Pour loyer du bien : {paiement.Contrat.Bien?.Reference} ({paiement.Contrat.Bien?.Quartier}, {paiement.Contrat.Bien?.Ville})");
                    col.Item().Text($"Mode de règlement : {paiement.ModePaiement} (Réf : {paiement.ReferenceTransactionOperateur})");
                    col.Item().Text($"Date d'encaissement : {paiement.DatePaiement:dd/MM/yyyy HH:mm}");
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text(config.PiedDePage).FontSize(8).Italic();
                    row.ConstantItem(100).AlignRight().Text("Le Gestionnaire").Bold();
                });
            });
        });

        return document.GeneratePdf();
    }
}