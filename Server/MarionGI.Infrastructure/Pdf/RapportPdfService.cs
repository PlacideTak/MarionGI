using MarionGI.Infrastructure.Pdf;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MarionGI.Infrastructure.Services;

public class RapportPdfService : IRapportPdfService
{
    private readonly IWebHostEnvironment _env;

    public RapportPdfService(IWebHostEnvironment env)
    {
        _env = env;
    }

    // =========================================================
    // GÉNÉRATION PRINCIPALE
    // =========================================================

    public byte[] GenererRapportPdf(
        string typeRapport,
        string titreRapport,
        JsonElement donneesBrutes)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var type = NormaliserTypeRapport(typeRapport);

        var titre =
            string.IsNullOrWhiteSpace(titreRapport)
                ? "Rapport"
                : titreRapport.Trim();

        var config = ChargerParametres();

        // ---------------------------------------------------------
        // Détection d'une éventuelle enveloppe JSON.
        //
        // Exemple :
        // {
        //   "data": {
        //      "Total": ...,
        //      "ParMode": [...]
        //   }
        // }
        //
        // ou :
        //
        // {
        //   "result": [...]
        // }
        // ---------------------------------------------------------

        donneesBrutes =
            ExtraireDonneesUtiles(donneesBrutes);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);

                page.Margin(30);

                page.PageColor(Colors.White);

                page.DefaultTextStyle(style =>
                    style
                        .FontSize(9)
                        .FontFamily("Arial"));

                // =================================================
                // EN-TÊTE
                // =================================================

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item()
                                .Text(config.NomSociete)
                                .Bold()
                                .FontSize(16)
                                .FontColor("#1F6F54");

                            if (!string.IsNullOrWhiteSpace(
                                    config.Adresse))
                            {
                                col.Item()
                                    .Text(config.Adresse)
                                    .FontSize(8)
                                    .FontColor(
                                        Colors.Grey.Darken2);
                            }

                            if (!string.IsNullOrWhiteSpace(
                                    config.Telephone))
                            {
                                col.Item()
                                    .Text(config.Telephone)
                                    .FontSize(8)
                                    .FontColor(
                                        Colors.Grey.Darken2);
                            }

                            if (!string.IsNullOrWhiteSpace(
                                    config.Email))
                            {
                                col.Item()
                                    .Text(config.Email)
                                    .FontSize(8)
                                    .FontColor(
                                        Colors.Grey.Darken2);
                            }
                        });

                        row.ConstantItem(110)
                            .AlignRight()
                            .Text(
                                DateTime.Now.ToString(
                                    "dd/MM/yyyy HH:mm",
                                    CultureInfo.CurrentCulture))
                            .FontSize(8)
                            .FontColor(
                                Colors.Grey.Darken2);
                    });

                    header.Item()
                        .PaddingTop(8)
                        .LineHorizontal(1)
                        .LineColor("#1F6F54");
                });

                // =================================================
                // CONTENU
                // =================================================

                page.Content()
                    .PaddingVertical(15)
                    .Column(col =>
                    {
                        col.Item()
                            .Text(titre)
                            .Bold()
                            .FontSize(14)
                            .FontColor("#1F6F54");

                        col.Item()
                            .PaddingTop(8)
                            .LineHorizontal(1)
                            .LineColor("#1F6F54");

                        col.Item()
                            .PaddingTop(12);

                        // -----------------------------------------
                        // Aucune donnée
                        // -----------------------------------------

                        if (donneesBrutes.ValueKind ==
                                JsonValueKind.Null ||
                            donneesBrutes.ValueKind ==
                                JsonValueKind.Undefined)
                        {
                            AfficherAucuneDonnee(col);
                            return;
                        }

                        // -----------------------------------------
                        // Objet
                        // -----------------------------------------

                        if (donneesBrutes.ValueKind ==
                            JsonValueKind.Object)
                        {
                            GenererObjet(
                                col,
                                type,
                                donneesBrutes);

                            return;
                        }

                        // -----------------------------------------
                        // Tableau
                        // -----------------------------------------

                        if (donneesBrutes.ValueKind ==
                            JsonValueKind.Array)
                        {
                            GenererTableau(
                                col,
                                type,
                                donneesBrutes);

                            return;
                        }

                        // -----------------------------------------
                        // Format invalide
                        // -----------------------------------------

                        col.Item()
                            .PaddingTop(15)
                            .Text(
                                "Le format des données du rapport est invalide.")
                            .FontColor(
                                Colors.Red.Medium);
                    });

                // =================================================
                // PIED DE PAGE
                // =================================================

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Document généré le ");

                        text.Span(
                                DateTime.Now.ToString(
                                    "dd/MM/yyyy HH:mm",
                                    CultureInfo.CurrentCulture))
                            .Bold();

                        text.Span(" — Page ");

                        text.CurrentPageNumber();

                        text.Span(" / ");

                        text.TotalPages();
                    });
            });
        });

        return document.GeneratePdf();
    }

    // =========================================================
    // NORMALISATION DU TYPE
    // =========================================================

    private static string NormaliserTypeRapport(
        string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return string.Empty;

        return type
            .Trim()
            .ToLowerInvariant()
            .Replace("_", "-")
            .Replace(" ", "-")
            .Replace("—", "-")
            .Replace("–", "-");
    }

    // =========================================================
    // EXTRACTION DONNÉES
    // =========================================================

    private static JsonElement ExtraireDonneesUtiles(
        JsonElement donnees)
    {
        if (donnees.ValueKind !=
            JsonValueKind.Object)
        {
            return donnees;
        }

        // data
        if (TryGetProperty(
                donnees,
                "data",
                out var data))
        {
            if (data.ValueKind ==
                    JsonValueKind.Object ||
                data.ValueKind ==
                    JsonValueKind.Array)
            {
                return data;
            }
        }

        // result
        if (TryGetProperty(
                donnees,
                "result",
                out var result))
        {
            if (result.ValueKind ==
                    JsonValueKind.Object ||
                result.ValueKind ==
                    JsonValueKind.Array)
            {
                return result;
            }
        }

        // donnees
        if (TryGetProperty(
                donnees,
                "donnees",
                out var donneesInternes))
        {
            if (donneesInternes.ValueKind ==
                    JsonValueKind.Object ||
                donneesInternes.ValueKind ==
                    JsonValueKind.Array)
            {
                return donneesInternes;
            }
        }

        return donnees;
    }

    // =========================================================
    // OBJET
    // =========================================================

    private void GenererObjet(
        ColumnDescriptor col,
        string type,
        JsonElement objet)
    {
        switch (type)
        {
            // -------------------------------------------------
            // HISTORIQUE LOCATAIRE
            // -------------------------------------------------

            case "historique-locataire":
            case "historique-paiements":
            case "historique-paiements-locataire":
            case "historique-paiement":

                GenererHistoriqueLocataire(
                    col,
                    objet);

                return;

            // -------------------------------------------------
            // ENCAISSEMENTS
            // -------------------------------------------------

            case "encaissements":
            case "encaissement":
            case "encaissements-par-mode":
            case "encaissements-par-mode-paiement":
            case "encaissement-par-mode":
            case "encaissement-par-mode-paiement":

                GenererEncaissements(
                    col,
                    objet);

                return;

            // -------------------------------------------------
            // CONTRATS
            // -------------------------------------------------

            case "contrats":
            case "rapport-contrats":

                GenererContrats(
                    col,
                    objet);

                return;

            // -------------------------------------------------
            // IMPAYÉS
            // -------------------------------------------------

            case "impayes":
            case "impayes-locataires":
            case "impayes-locataire":
            case "impaye":

                GenererImpayes(
                    col,
                    objet);

                return;

            // -------------------------------------------------
            // RAPPORT GÉNÉRIQUE
            // -------------------------------------------------

            default:

                GenererObjetGenerique(
                    col,
                    objet);

                return;
        }
    }

    // =========================================================
    // TABLEAU
    // =========================================================

    private void GenererTableau(
        ColumnDescriptor col,
        string type,
        JsonElement tableau)
    {
        if (tableau.ValueKind !=
                JsonValueKind.Array ||
            tableau.GetArrayLength() == 0)
        {
            AfficherAucuneDonnee(col);
            return;
        }

        switch (type)
        {
            case "historique-locataire":
            case "historique-paiements":
            case "historique-paiements-locataire":
            case "historique-paiement":

                GenererTableauGenerique(
                    col,
                    tableau,
                    new[]
                    {
                        "datePaiement",
                        "montant",
                        "modePaiement",
                        "referenceContrat",
                        "uniteReference",
                        "bienNom"
                    });

                return;

            case "encaissements":
            case "encaissement":
            case "encaissements-par-mode":
            case "encaissements-par-mode-paiement":
            case "encaissement-par-mode":
            case "encaissement-par-mode-paiement":

                GenererTableauGenerique(
                    col,
                    tableau,
                    new[]
                    {
                        "datePaiement",
                        "montant",
                        "modePaiement",
                        "uniteReference",
                        "bienNom",
                        "bienReference",
                        "locataireNom"
                    });

                return;

            case "contrats":
            case "rapport-contrats":

                GenererContrats(
                    col,
                    tableau);

                return;

            case "impayes":
            case "impayes-locataires":
            case "impayes-locataire":
            case "impaye":

                GenererImpayes(
                    col,
                    tableau);

                return;

            default:

                GenererTableauGenerique(
                    col,
                    tableau);

                return;
        }
    }

    // =========================================================
    // HISTORIQUE LOCATAIRE
    // =========================================================

    private void GenererHistoriqueLocataire(
        ColumnDescriptor col,
        JsonElement objet)
    {
        if (TryGetProperty(
                objet,
                "locataire",
                out var locataire) &&
            locataire.ValueKind ==
                JsonValueKind.Object)
        {
            col.Item()
                .PaddingBottom(10)
                .Text("Locataire")
                .Bold()
                .FontSize(11)
                .FontColor("#1F6F54");

            AfficherLigne(
                col,
                "Nom",
                $"{GetString(locataire, "prenom")} " +
                $"{GetString(locataire, "nom")}".Trim());

            AfficherLigne(
                col,
                "Email",
                GetString(locataire, "email"));

            AfficherLigne(
                col,
                "Téléphone",
                GetString(locataire, "telephone"));
        }

        // -----------------------------------------------------
        // Historique retourné directement par le contrôleur :
        //
        // {
        //   Locataire: ...,
        //   TotalVersements: ...,
        //   NombrePaiements: ...,
        //   Historique: [...]
        // }
        // -----------------------------------------------------

        if (TryGetProperty(
                objet,
                "historique",
                out var historique) &&
            historique.ValueKind ==
                JsonValueKind.Array)
        {
            col.Item()
                .PaddingTop(15)
                .PaddingBottom(6)
                .Text("Historique des paiements")
                .Bold()
                .FontSize(11)
                .FontColor("#1F6F54");

            GenererTableauGenerique(
                col,
                historique,
                new[]
                {
                    "datePaiement",
                    "montant",
                    "modePaiement",
                    "numeroQuittance",
                    "uniteReference",
                    "bienNom",
                    "bienReference"
                });

            return;
        }

        // -----------------------------------------------------
        // Ancienne structure : paiements
        // -----------------------------------------------------

        if (TryGetProperty(
                objet,
                "paiements",
                out var paiements) &&
            paiements.ValueKind ==
                JsonValueKind.Array)
        {
            col.Item()
                .PaddingTop(15)
                .PaddingBottom(6)
                .Text("Historique des paiements")
                .Bold()
                .FontSize(11)
                .FontColor("#1F6F54");

            GenererTableauGenerique(
                col,
                paiements,
                new[]
                {
                    "datePaiement",
                    "montant",
                    "modePaiement",
                    "numeroQuittance",
                    "uniteReference",
                    "bienNom",
                    "bienReference"
                });

            return;
        }

        // -----------------------------------------------------
        // Contrats
        // -----------------------------------------------------

        if (TryGetProperty(
                objet,
                "contrats",
                out var contrats) &&
            contrats.ValueKind ==
                JsonValueKind.Array &&
            contrats.GetArrayLength() > 0)
        {
            col.Item()
                .PaddingTop(15)
                .PaddingBottom(6)
                .Text("Contrats")
                .Bold()
                .FontSize(11)
                .FontColor("#1F6F54");

            GenererTableauGenerique(
                col,
                contrats);

            return;
        }

        AfficherAucuneDonnee(col);
    }

    // =========================================================
    // ENCAISSEMENTS
    // =========================================================

    private void GenererEncaissements(
        ColumnDescriptor col,
        JsonElement donnees)
    {
        // -----------------------------------------------------
        // CAS OBJET
        // -----------------------------------------------------

        if (donnees.ValueKind ==
            JsonValueKind.Object)
        {
            // Date début
            if (TryGetProperty(
                    donnees,
                    "DateDebut",
                    out var dateDebut))
            {
                AfficherLigne(
                    col,
                    "Date de début",
                    FormaterDate(dateDebut));
            }

            // Date fin
            if (TryGetProperty(
                    donnees,
                    "DateFin",
                    out var dateFin))
            {
                AfficherLigne(
                    col,
                    "Date de fin",
                    FormaterDate(dateFin));
            }

            // Total
            if (TryGetProperty(
                    donnees,
                    "Total",
                    out var total))
            {
                AfficherLigne(
                    col,
                    "Total encaissé",
                    FormaterMontant(total));
            }

            // Nombre transactions
            if (TryGetProperty(
                    donnees,
                    "NombreTransactions",
                    out var nombreTransactions))
            {
                AfficherLigne(
                    col,
                    "Nombre de transactions",
                    JsonElementToString(
                        nombreTransactions));
            }

            // -------------------------------------------------
            // ParMode
            // -------------------------------------------------

            if (TryGetProperty(
                    donnees,
                    "ParMode",
                    out var parMode) &&
                parMode.ValueKind ==
                    JsonValueKind.Array)
            {
                GenererEncaissementsParMode(
                    col,
                    parMode);

                return;
            }

            // -------------------------------------------------
            // paiements
            // -------------------------------------------------

            if (TryGetProperty(
                    donnees,
                    "paiements",
                    out var paiements) &&
                paiements.ValueKind ==
                    JsonValueKind.Array)
            {
                GenererTableauGenerique(
                    col,
                    paiements,
                    new[]
                    {
                        "datePaiement",
                        "montant",
                        "modePaiement",
                        "uniteReference",
                        "bienNom",
                        "bienReference",
                        "locataireNom"
                    });

                return;
            }

            // -------------------------------------------------
            // historique
            // -------------------------------------------------

            if (TryGetProperty(
                    donnees,
                    "historique",
                    out var historique) &&
                historique.ValueKind ==
                    JsonValueKind.Array)
            {
                GenererTableauGenerique(
                    col,
                    historique);

                return;
            }

            // -------------------------------------------------
            // Si aucune structure connue :
            // afficher l'objet au lieu d'avoir un PDF vide.
            // -------------------------------------------------

            GenererObjetGenerique(
                col,
                donnees);

            return;
        }

        // -----------------------------------------------------
        // CAS TABLEAU
        // -----------------------------------------------------

        if (donnees.ValueKind ==
            JsonValueKind.Array)
        {
            if (donnees.GetArrayLength() == 0)
            {
                AfficherAucuneDonnee(col);
                return;
            }

            GenererTableauGenerique(
                col,
                donnees);

            return;
        }

        AfficherAucuneDonnee(col);
    }

    // =========================================================
    // ENCAISSEMENTS PAR MODE
    // =========================================================

    private void GenererEncaissementsParMode(
        ColumnDescriptor col,
        JsonElement parMode)
    {
        col.Item()
            .PaddingTop(15)
            .PaddingBottom(8)
            .Text("Encaissements par mode de paiement")
            .Bold()
            .FontSize(11)
            .FontColor("#1F6F54");

        if (parMode.ValueKind !=
                JsonValueKind.Array ||
            parMode.GetArrayLength() == 0)
        {
            AfficherAucuneDonnee(col);
            return;
        }

        // =====================================================
        // SYNTHÈSE
        // =====================================================

        col.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
            });

            table.Cell()
                .Element(HeaderCellStyle)
                .Text("Mode de paiement");

            table.Cell()
                .Element(HeaderCellStyle)
                .Text("Transactions");

            table.Cell()
                .Element(HeaderCellStyle)
                .Text("Total encaissé");

            foreach (var mode in
                     parMode.EnumerateArray())
            {
                if (mode.ValueKind !=
                    JsonValueKind.Object)
                {
                    continue;
                }

                var modePaiement =
                    GetString(
                        mode,
                        "ModePaiement");

                var nombreTransactions =
                    GetInt32(
                        mode,
                        "NombreTransactions");

                var totalRecette =
                    GetDecimal(
                        mode,
                        "TotalRecette");

                table.Cell()
                    .Element(DataCellStyle)
                    .Text(
                        FormaterModePaiement(
                            modePaiement));

                table.Cell()
                    .Element(DataCellStyle)
                    .Text(
                        nombreTransactions.ToString(
                            CultureInfo.CurrentCulture));

                table.Cell()
                    .Element(DataCellStyle)
                    .Text(
                        FormaterMontant(
                            totalRecette));
            }
        });

        // =====================================================
        // DÉTAILS
        // =====================================================

        foreach (var mode in
                 parMode.EnumerateArray())
        {
            if (mode.ValueKind !=
                JsonValueKind.Object)
            {
                continue;
            }

            var modePaiement =
                GetString(
                    mode,
                    "ModePaiement");

            if (!TryGetProperty(
                    mode,
                    "Details",
                    out var details) ||
                details.ValueKind !=
                    JsonValueKind.Array ||
                details.GetArrayLength() == 0)
            {
                continue;
            }

            col.Item()
                .PaddingTop(18)
                .PaddingBottom(6)
                .Text(
                    $"Détails — " +
                    $"{FormaterModePaiement(modePaiement)}")
                .Bold()
                .FontSize(10)
                .FontColor("#1F6F54");

            GenererTableauGenerique(
                col,
                details,
                new[]
                {
                    "datePaiement",
                    "montant",
                    "uniteReference",
                    "bienNom",
                    "bienReference",
                    "locataireNom"
                });
        }
    }

    // =========================================================
    // CONTRATS
    // =========================================================

    private static void GenererContrats(
     ColumnDescriptor col,
     JsonElement donnees)
    {
        // ========================================================
        // RÉCUPÉRATION DU TABLEAU
        // ========================================================

        JsonElement contrats = donnees;

        if (donnees.ValueKind == JsonValueKind.Object &&
            TryGetProperty(
                donnees,
                "contrats",
                out var contratsProperty) &&
            contratsProperty.ValueKind == JsonValueKind.Array)
        {
            contrats = contratsProperty;
        }

        // ========================================================
        // VÉRIFICATION
        // ========================================================

        if (contrats.ValueKind != JsonValueKind.Array ||
            !contrats.EnumerateArray().Any())
        {
            AfficherAucuneDonnee(col);
            return;
        }

        // ========================================================
        // COLONNES
        // ========================================================

        GenererTableauGenerique(
            col,
            contrats,
            new[]
            {
            "uniteReference",
            "bienNom",
            "bienReference",
            "bienAdresse",
            "locataireNom",
            "dateDebut",
            "dateFin",
            "montantLoyer",
            "montantCaution",
            "statutContrat"
            });
    }

    // =========================================================
    // IMPAYÉS
    // =========================================================

    private void GenererImpayes(
        ColumnDescriptor col,
        JsonElement donnees)
    {
        if (donnees.ValueKind ==
            JsonValueKind.Object)
        {
            if (TryGetProperty(
                    donnees,
                    "Impayes",
                    out var impayes) &&
                impayes.ValueKind ==
                    JsonValueKind.Array)
            {
                donnees = impayes;
            }
            else
            {
                GenererObjetGenerique(
                    col,
                    donnees);

                return;
            }
        }

        if (donnees.ValueKind !=
                JsonValueKind.Array ||
            donnees.GetArrayLength() == 0)
        {
            AfficherAucuneDonnee(col);
            return;
        }

        GenererTableauGenerique(
            col,
            donnees,
            new[]
            {
                "ContratId",
                "UniteReference",
                "BienNom",
                "BienReference",
                "LocataireNom",
                "LocataireTelephone",
                "MontantLoyer",
                "MontantPaye",
                "Solde",
                "DelaiToleranceJours",
                "DateLimite",
                "JoursDeRetard"
            });
    }

    // =========================================================
    // TABLEAU GÉNÉRIQUE
    // =========================================================

    private static void GenererTableauGenerique(
        ColumnDescriptor col,
        JsonElement tableau,
        string[]? colonnes = null)
    {
        if (tableau.ValueKind !=
                JsonValueKind.Array ||
            tableau.GetArrayLength() == 0)
        {
            AfficherAucuneDonnee(col);
            return;
        }

        var elements =
            tableau
                .EnumerateArray()
                .Where(x =>
                    x.ValueKind ==
                    JsonValueKind.Object)
                .ToList();

        if (elements.Count == 0)
        {
            AfficherAucuneDonnee(col);
            return;
        }

        var proprietes =
            colonnes?.Length > 0
                ? colonnes
                : elements
                    .First()
                    .EnumerateObject()
                    .Select(x => x.Name)
                    .ToArray();

        if (proprietes.Length == 0)
        {
            AfficherAucuneDonnee(col);
            return;
        }

        col.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var _ in proprietes)
                {
                    columns.RelativeColumn();
                }
            });

            // -------------------------------------------------
            // EN-TÊTES
            // -------------------------------------------------

            foreach (var propriete in
                     proprietes)
            {
                table.Cell()
                    .Element(HeaderCellStyle)
                    .Text(
                        FormaterNomColonne(
                            propriete));
            }

            // -------------------------------------------------
            // LIGNES
            // -------------------------------------------------

            foreach (var element in elements)
            {
                foreach (var propriete in
                         proprietes)
                {
                    string valeur = string.Empty;

                    if (TryGetProperty(
                            element,
                            propriete,
                            out var property))
                    {
                        valeur =
                            FormaterValeur(
                                propriete,
                                property);
                    }

                    table.Cell()
                        .Element(DataCellStyle)
                        .Text(
                            string.IsNullOrWhiteSpace(
                                valeur)
                                ? "-"
                                : valeur);
                }
            }
        });
    }

    // =========================================================
    // OBJET GÉNÉRIQUE
    // =========================================================

    private void GenererObjetGenerique(
        ColumnDescriptor col,
        JsonElement objet)
    {
        if (objet.ValueKind !=
            JsonValueKind.Object)
        {
            col.Item()
                .Text(
                    JsonElementToString(objet));

            return;
        }

        var proprietes =
            objet.EnumerateObject().ToList();

        if (proprietes.Count == 0)
        {
            AfficherAucuneDonnee(col);
            return;
        }

        foreach (var property in proprietes)
        {
            if (property.Value.ValueKind ==
                JsonValueKind.Array)
            {
                col.Item()
                    .PaddingTop(10)
                    .Text(
                        FormaterNomColonne(
                            property.Name))
                    .Bold()
                    .FontColor("#1F6F54");

                GenererTableauGenerique(
                    col,
                    property.Value);

                continue;
            }

            if (property.Value.ValueKind ==
                JsonValueKind.Object)
            {
                col.Item()
                    .PaddingTop(10)
                    .Text(
                        FormaterNomColonne(
                            property.Name))
                    .Bold()
                    .FontColor("#1F6F54");

                GenererObjetGenerique(
                    col,
                    property.Value);

                continue;
            }

            AfficherLigne(
                col,
                FormaterNomColonne(
                    property.Name),
                FormaterValeur(
                    property.Name,
                    property.Value));
        }
    }

    // =========================================================
    // LIGNE
    // =========================================================

    private static void AfficherLigne(
        ColumnDescriptor col,
        string libelle,
        string valeur)
    {
        col.Item()
            .PaddingBottom(4)
            .Row(row =>
            {
                row.ConstantItem(150)
                    .Text(libelle)
                    .Bold();

                row.RelativeItem()
                    .Text(
                        string.IsNullOrWhiteSpace(
                            valeur)
                            ? "-"
                            : valeur);
            });
    }

    // =========================================================
    // AUCUNE DONNÉE
    // =========================================================

    private static void AfficherAucuneDonnee(
        ColumnDescriptor col)
    {
        col.Item()
            .PaddingTop(20)
            .AlignCenter()
            .Text(
                "Aucune donnée à afficher.")
            .Italic()
            .FontColor(
                Colors.Grey.Darken1);
    }

    // =========================================================
    // STYLE EN-TÊTE
    // =========================================================

    private static IContainer HeaderCellStyle(
        IContainer container)
    {
        return container
            .Background("#E8F3EE")
            .Border(1)
            .BorderColor("#B8CEC4")
            .PaddingVertical(5)
            .PaddingHorizontal(6)
            .DefaultTextStyle(style =>
                style
                    .Bold()
                    .FontSize(8));
    }

    // =========================================================
    // STYLE CELLULE
    // =========================================================

    private static IContainer DataCellStyle(
        IContainer container)
    {
        return container
            .Border(1)
            .BorderColor("#D9D9D9")
            .PaddingVertical(5)
            .PaddingHorizontal(6)
            .DefaultTextStyle(style =>
                style.FontSize(8));
    }

    // =========================================================
    // FORMATAGE VALEUR
    // =========================================================

    private static string FormaterValeur(
        string nomPropriete,
        JsonElement valeur)
    {
        if (valeur.ValueKind ==
                JsonValueKind.Null ||
            valeur.ValueKind ==
                JsonValueKind.Undefined)
        {
            return string.Empty;
        }

        var nom =
            nomPropriete
                .ToLowerInvariant()
                .Replace("_", "")
                .Replace("-", "");

        if (nom.Contains("date"))
        {
            return FormaterDate(valeur);
        }

        if (nom.Contains("montant") ||
            nom.Contains("total") ||
            nom.Contains("loyer") ||
            nom.Contains("recette") ||
            nom.Contains("solde") ||
            nom.Contains("impaye") ||
            nom.Contains("paye"))
        {
            return FormaterMontant(valeur);
        }

        if (nom == "modepaiement")
        {
            return FormaterModePaiement(
                JsonElementToString(valeur));
        }

        return JsonElementToString(valeur);
    }

    // =========================================================
    // FORMATAGE MONTANT
    // =========================================================

    private static string FormaterMontant(
        decimal montant)
    {
        return montant.ToString(
                   "N0",
                   CultureInfo.CurrentCulture)
               + " FCFA";
    }

    private static string FormaterMontant(
        JsonElement element)
    {
        if (element.ValueKind ==
                JsonValueKind.Number &&
            element.TryGetDecimal(
                out var montant))
        {
            return FormaterMontant(
                montant);
        }

        var texte =
            JsonElementToString(element);

        if (decimal.TryParse(
                texte,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out montant))
        {
            return FormaterMontant(
                montant);
        }

        if (decimal.TryParse(
                texte,
                NumberStyles.Any,
                CultureInfo.CurrentCulture,
                out montant))
        {
            return FormaterMontant(
                montant);
        }

        return texte;
    }

    // =========================================================
    // FORMATAGE DATE
    // =========================================================

    private static string FormaterDate(
        JsonElement element)
    {
        if (element.ValueKind ==
                JsonValueKind.Null ||
            element.ValueKind ==
                JsonValueKind.Undefined)
        {
            return "-";
        }

        if (element.ValueKind ==
                JsonValueKind.String &&
            element.TryGetDateTime(
                out var date))
        {
            return date.ToString(
                "dd/MM/yyyy",
                CultureInfo.CurrentCulture);
        }

        var texte =
            JsonElementToString(element);

        if (DateTime.TryParse(
                texte,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date))
        {
            return date.ToString(
                "dd/MM/yyyy",
                CultureInfo.CurrentCulture);
        }

        if (DateTime.TryParse(
                texte,
                CultureInfo.CurrentCulture,
                DateTimeStyles.None,
                out date))
        {
            return date.ToString(
                "dd/MM/yyyy",
                CultureInfo.CurrentCulture);
        }

        return texte;
    }

    // =========================================================
    // MODE PAIEMENT
    // =========================================================

    private static string FormaterModePaiement(
        string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
            return "-";

        return mode.Trim()
            .ToLowerInvariant() switch
        {
            "especes" => "Espèces",
            "espèces" => "Espèces",

            "mobilemoney" => "Mobile Money",
            "mobile_money" => "Mobile Money",
            "mobile money" => "Mobile Money",

            "virement" => "Virement",

            "cheque" => "Chèque",
            "chèque" => "Chèque",

            _ => mode
        };
    }

    // =========================================================
    // NOM COLONNE
    // =========================================================

    private static string FormaterNomColonne(
        string nom)
    {
        if (string.IsNullOrWhiteSpace(nom))
            return string.Empty;

        var result =
            nom.Replace("_", " ")
               .Replace("-", " ");

        result =
            Regex.Replace(
                result,
                "(\\B[A-Z])",
                " $1");

        return CultureInfo.CurrentCulture.TextInfo
            .ToTitleCase(
                result.ToLowerInvariant());
    }

    // =========================================================
    // JSON PROPERTY
    // =========================================================

    private static bool TryGetProperty(
        JsonElement objet,
        string nom,
        out JsonElement valeur)
    {
        valeur = default;

        if (objet.ValueKind !=
            JsonValueKind.Object)
        {
            return false;
        }

        if (objet.TryGetProperty(
                nom,
                out valeur))
        {
            return true;
        }

        foreach (var property in
                 objet.EnumerateObject())
        {
            if (string.Equals(
                    property.Name,
                    nom,
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                valeur =
                    property.Value;

                return true;
            }
        }

        return false;
    }

    // =========================================================
    // STRING
    // =========================================================

    private static string GetString(
        JsonElement objet,
        string nom)
    {
        if (!TryGetProperty(
                objet,
                nom,
                out var valeur))
        {
            return string.Empty;
        }

        return JsonElementToString(
            valeur);
    }

    // =========================================================
    // DECIMAL
    // =========================================================

    private static decimal GetDecimal(
        JsonElement objet,
        string nom)
    {
        if (!TryGetProperty(
                objet,
                nom,
                out var valeur))
        {
            return 0m;
        }

        if (valeur.ValueKind ==
                JsonValueKind.Number &&
            valeur.TryGetDecimal(
                out var result))
        {
            return result;
        }

        var texte =
            JsonElementToString(valeur);

        if (decimal.TryParse(
                texte,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out result))
        {
            return result;
        }

        if (decimal.TryParse(
                texte,
                NumberStyles.Any,
                CultureInfo.CurrentCulture,
                out result))
        {
            return result;
        }

        return 0m;
    }

    // =========================================================
    // INT
    // =========================================================

    private static int GetInt32(
        JsonElement objet,
        string nom)
    {
        if (!TryGetProperty(
                objet,
                nom,
                out var valeur))
        {
            return 0;
        }

        if (valeur.ValueKind ==
                JsonValueKind.Number &&
            valeur.TryGetInt32(
                out var result))
        {
            return result;
        }

        var texte =
            JsonElementToString(valeur);

        return int.TryParse(
            texte,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out result)
                ? result
                : 0;
    }

    // =========================================================
    // JSON → STRING
    // =========================================================

    private static string JsonElementToString(
        JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:

                return string.Empty;

            case JsonValueKind.String:

                return element.GetString()
                       ?? string.Empty;

            case JsonValueKind.Number:

                if (element.TryGetDecimal(
                        out var decimalValue))
                {
                    return decimalValue.ToString(
                        "N2",
                        CultureInfo.CurrentCulture);
                }

                return element.ToString();

            case JsonValueKind.True:

                return "Oui";

            case JsonValueKind.False:

                return "Non";

            case JsonValueKind.Array:
            case JsonValueKind.Object:

                return element.GetRawText();

            default:

                return element.ToString();
        }
    }

    // =========================================================
    // CONFIGURATION
    // =========================================================

    private sealed class ParametresPdf
    {
        public string NomSociete { get; set; }
            = "MarionGI";

        public string Adresse { get; set; }
            = string.Empty;

        public string Telephone { get; set; }
            = string.Empty;

        public string Email { get; set; }
            = string.Empty;
    }

    private ParametresPdf ChargerParametres()
    {
        return new ParametresPdf
        {
            NomSociete = "MarionGI",
            Adresse = string.Empty,
            Telephone = string.Empty,
            Email = string.Empty
        };
    }
}