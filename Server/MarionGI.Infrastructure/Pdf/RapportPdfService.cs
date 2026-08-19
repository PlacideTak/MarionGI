using MarionGI.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using MarionGI.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;

namespace MarionGI.Infrastructure.Pdf;

public class RapportPdfService : IRapportPdfService
{
    private readonly IWebHostEnvironment _env;

    public RapportPdfService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public byte[] GenererRapportPdf(string typeRapport, string titreRapport, JsonElement donneesBrutes)
    {
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
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                // EN-TÊTE
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(config.NomSociete ?? "MarionGI").Bold().FontSize(14).FontColor("#1F6F54");
                        col.Item().Text(config.Adresse).FontSize(8);
                        col.Item().Text(config.Telephone).FontSize(8);
                    });

                    row.ConstantItem(180).AlignRight().Column(col =>
                    {
                        col.Item().Text("Rapport Officiel").Bold().FontSize(11).FontColor("#1F6F54");
                        col.Item().Text($"Date : {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8);
                    });
                });

                // CONTENU
                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Item().LineHorizontal(1).LineColor("#1F6F54");
                    col.Item().PaddingVertical(8).Text(titreRapport).Bold().FontSize(13).FontColor("#1F6F54");

                    // --- CAS 1 : OBJET UNIQUE (ex: Historique Locataire) ---
                    if (donneesBrutes.ValueKind == JsonValueKind.Object && typeRapport == "historique-locataire")
                    {
                        if (donneesBrutes.TryGetProperty("locataire", out var locProp))
                        {
                            // Récupération du nom et du prénom
                            string nomLoc = locProp.TryGetProperty("nom", out var n) ? n.GetString() ?? "" : "";
                            string prenomLoc = locProp.TryGetProperty("prenom", out var p) ? p.GetString() ?? "" : "";
                            string emailLoc = locProp.TryGetProperty("email", out var e) ? e.GetString() ?? "" : "";
                            string telLoc = locProp.TryGetProperty("telephone", out var t) ? t.GetString() ?? "" : "";

                            // Affichage concaténé : Prénom Nom
                            col.Item().PaddingBottom(5)
                               .Text($"Locataire : {prenomLoc} {nomLoc}".Trim())
                               .Bold().FontSize(11).FontColor("#1F6F54");

                            col.Item().PaddingBottom(10)
                               .Text($"Email : {emailLoc} | Téléphone : {telLoc}")
                               .FontSize(9);
                        }

                        if (donneesBrutes.TryGetProperty("totalVersements", out var tvProp))
                        {
                            decimal total = tvProp.GetDecimal();
                            col.Item().PaddingBottom(10).Text($"Total des versements confirmés : {total:N0} FCFA").Bold().FontSize(10);
                        }

                        if (donneesBrutes.TryGetProperty("historique", out var histProp) && histProp.ValueKind == JsonValueKind.Array)
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(75);
                                    columns.RelativeColumn(1);
                                    columns.ConstantColumn(70);
                                    columns.ConstantColumn(80);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#1F6F54").Padding(5).Text("Date").FontColor(Colors.White).Bold();
                                    header.Cell().Background("#1F6F54").Padding(5).Text("Montant").FontColor(Colors.White).Bold();
                                    header.Cell().Background("#1F6F54").Padding(5).Text("Statut").FontColor(Colors.White).Bold();
                                    header.Cell().Background("#1F6F54").Padding(5).Text("Mode").FontColor(Colors.White).Bold();
                                    header.Cell().Background("#1F6F54").Padding(5).Text("Bien").FontColor(Colors.White).Bold();
                                });

                                foreach (var item in histProp.EnumerateArray())
                                {
                                    string dateP = item.TryGetProperty("datePaiement", out var dp) && DateTime.TryParse(dp.GetString(), out var dt) ? dt.ToString("dd/MM/yyyy") : "";
                                    decimal montant = item.TryGetProperty("montant", out var m) ? m.GetDecimal() : 0;
                                    string statut = item.TryGetProperty("statut", out var s) ? s.GetString() ?? "" : "";
                                    string mode = item.TryGetProperty("modePaiement", out var mp) ? mp.GetString() ?? "" : "";
                                    string bienRef = item.TryGetProperty("bienReference", out var br) ? br.GetString() ?? "" : "";

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(dateP);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{montant:N0} FCFA");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(statut);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(mode);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(bienRef);
                                }
                            });
                        }
                    }
                    // --- CAS 2 : TABLEAUX ---
                    else if (donneesBrutes.ValueKind == JsonValueKind.Array)
                    {
                        // --- RAPPORT A : ENCAISSEMENTS PAR MODE ---
                        if (typeRapport == "encaissements")
                        {
                            foreach (var groupe in donneesBrutes.EnumerateArray())
                            {
                                string modePaiement = groupe.TryGetProperty("modePaiement", out var mp) ? mp.GetString() ?? "Inconnu" : "Inconnu";
                                decimal totalRecette = groupe.TryGetProperty("totalRecette", out var tr) ? tr.GetDecimal() : 0;

                                col.Item().PaddingTop(8).Text($"Mode de paiement : {modePaiement} (Total : {totalRecette:N0} FCFA)").Bold().FontSize(10);

                                if (groupe.TryGetProperty("details", out var details) && details.ValueKind == JsonValueKind.Array)
                                {
                                    col.Item().PaddingTop(4).Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.ConstantColumn(70);
                                            columns.RelativeColumn();
                                            columns.RelativeColumn();
                                            columns.ConstantColumn(80);
                                        });

                                        table.Header(header =>
                                        {
                                            header.Cell().Background("#1F6F54").Padding(4).Text("Date").FontColor(Colors.White).Bold();
                                            header.Cell().Background("#1F6F54").Padding(4).Text("Bien").FontColor(Colors.White).Bold();
                                            header.Cell().Background("#1F6F54").Padding(4).Text("Locataire").FontColor(Colors.White).Bold();
                                            header.Cell().Background("#1F6F54").Padding(4).Text("Montant").FontColor(Colors.White).Bold();
                                        });

                                        foreach (var item in details.EnumerateArray())
                                        {
                                            string datePaiement = item.TryGetProperty("datePaiement", out var dp) && DateTime.TryParse(dp.GetString(), out var dt) ? dt.ToString("dd/MM/yyyy") : "";
                                            string bienRef = item.TryGetProperty("bienReference", out var br) ? br.GetString() ?? "" : "";
                                            string locataireNom = item.TryGetProperty("locataireNom", out var ln) ? ln.GetString() ?? "" : "";
                                            decimal montant = item.TryGetProperty("montant", out var m) ? m.GetDecimal() : 0;

                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(datePaiement);
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(bienRef);
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(locataireNom);
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{montant:N0} FCFA");
                                        }
                                    });
                                }
                            }
                        }
                        // --- RAPPORT B : CONTRATS ---
                        else if (typeRapport == "contrats")
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1.2f);
                                    columns.RelativeColumn(1.5f);
                                    columns.ConstantColumn(60);
                                    columns.ConstantColumn(60);
                                    columns.ConstantColumn(70);
                                    columns.ConstantColumn(60);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#1F6F54").Padding(4).Text("Bien").FontColor(Colors.White).Bold();
                                    header.Cell().Background("#1F6F54").Padding(4).Text("Locataire").FontColor(Colors.White).Bold();
                                    header.Cell().Background("#1F6F54").Padding(4).Text("Début").FontColor(Colors.White).Bold();
                                    header.Cell().Background("#1F6F54").Padding(4).Text("Fin").FontColor(Colors.White).Bold();
                                    header.Cell().Background("#1F6F54").Padding(4).Text("Loyer").FontColor(Colors.White).Bold();
                                    header.Cell().Background("#1F6F54").Padding(4).Text("Statut").FontColor(Colors.White).Bold();
                                });

                                foreach (var row in donneesBrutes.EnumerateArray())
                                {
                                    string bien = row.TryGetProperty("bienReference", out var b) ? b.GetString() ?? "" : "";
                                    string locataire = row.TryGetProperty("locataireNom", out var l) ? l.GetString() ?? "" : "";
                                    string debut = row.TryGetProperty("dateDebut", out var dd) && DateTime.TryParse(dd.GetString(), out var ddt) ? ddt.ToString("dd/MM/yyyy") : "";
                                    string fin = row.TryGetProperty("dateFin", out var df) && DateTime.TryParse(df.GetString(), out var dft) ? dft.ToString("dd/MM/yyyy") : "";
                                    decimal loyer = row.TryGetProperty("montantLoyer", out var ml) ? ml.GetDecimal() : 0;
                                    string statut = row.TryGetProperty("statutContrat", out var sc) ? sc.GetString() ?? "" : "";

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(bien);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(locataire);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(debut);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(fin);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{loyer:N0}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(statut);
                                }
                            });
                        }
                        // --- RAPPORT C : IMPAYÉS ---
                        else if (typeRapport == "impayes")
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1.2f);
                                    columns.RelativeColumn(1.5f);
                                    columns.ConstantColumn(80);
                                    columns.ConstantColumn(70);
                                    columns.ConstantColumn(60);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#1F6F54").Padding(4).Text("Bien").FontColor(Colors.White).Bold();
                                    header.Cell().Background("#1F6F54").Padding(4).Text("Locataire").FontColor(Colors.White).Bold();
                                    header.Cell().Background("#1F6F54").Padding(4).Text("Téléphone").FontColor(Colors.White).Bold();
                                    header.Cell().Background("#1F6F54").Padding(4).Text("Loyer dû").FontColor(Colors.White).Bold();
                                    header.Cell().Background("#1F6F54").Padding(4).Text("Retard").FontColor(Colors.White).Bold();
                                });

                                foreach (var row in donneesBrutes.EnumerateArray())
                                {
                                    string bien = row.TryGetProperty("bienReference", out var b) ? b.GetString() ?? "" : "";
                                    string locataire = row.TryGetProperty("locataireNom", out var l) ? l.GetString() ?? "" : "";
                                    string tel = row.TryGetProperty("locataireTelephone", out var t) ? t.GetString() ?? "" : "";
                                    decimal loyer = row.TryGetProperty("montantLoyer", out var ml) ? ml.GetDecimal() : 0;
                                    int retard = row.TryGetProperty("joursDeRetard", out var jr) ? jr.GetInt32() : 0;

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(bien);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(locataire);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(tel);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{loyer:N0}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{retard} j");
                                }
                            });
                        }
                    }
                });

                // PIED DE PAGE
                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text(config.PiedDePage ?? "MarionGI - Gestion Immobilière").FontSize(8).Italic();
                    row.ConstantItem(80).AlignRight().Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}