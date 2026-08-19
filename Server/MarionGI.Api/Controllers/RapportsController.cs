using MarionGI.Domain.Enums;
using MarionGI.Infrastructure.Pdf;
using MarionGI.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;

namespace MarionGI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = TextesMessages.DroitAdministrateur + "," + TextesMessages.DroitGestionnaire + "," + TextesMessages.DroitProprietaire)]
public class RapportsController : ControllerBase
{
    private readonly MarionDbContext _context;
    private readonly IRapportPdfService _rapportPdfService;

    public RapportsController(MarionDbContext context, IRapportPdfService rapportPdfService)
    {
        _context = context;
        _rapportPdfService = rapportPdfService;
    }

    [HttpGet("dashboard-kpi")]
    public async Task<IActionResult> GetKpis()
    {
        try
        {
            var maintenant = DateTime.UtcNow;

            // 1. Biens & Occupation
            var totalBiens = await _context.Biens.CountAsync();
            var biensLoues = await _context.Biens.CountAsync(b => b.Statut == StatutBien.Loue);
            var tauxOccupation = totalBiens > 0 ? (double)biensLoues / totalBiens * 100 : 0;

            // 2. Encaissements du mois en cours
            var encaissementsMois = await _context.Paiements
                .Where(p => p.StatutTransaction == StatutTransaction.Confirme
                         && p.DatePaiement.Month == maintenant.Month
                         && p.DatePaiement.Year == maintenant.Year)
                .SumAsync(p => (decimal?)p.Montant) ?? 0m;

            // 3. Calcul de l'évolution des 12 derniers mois pour le graphique
            var labels = new List<string>();
            var datasets = new List<decimal>();

            var anneeEnCours = maintenant.Year;
            for (int mois = 1; mois <= 12; mois++)
            {
                var debutMois = new DateTime(anneeEnCours, mois, 1, 0, 0, 0, DateTimeKind.Utc);
                var finMois = debutMois.AddMonths(1).AddTicks(-1);

                labels.Add(debutMois.ToString("MMM", new CultureInfo("fr-FR")));

                var totalMois = await _context.Paiements
                    .Where(p => p.StatutTransaction == StatutTransaction.Confirme
                             && p.DatePaiement >= debutMois
                             && p.DatePaiement <= finMois)
                    .SumAsync(p => (decimal?)p.Montant) ?? 0m;

                datasets.Add(totalMois);
            }

            // 4. Génération dynamique des alertes basées sur les Contrats actifs
            var alertesList = new List<object>();

            var contratsActifs = await _context.Contrats
                .Include(c => c.Bien)
                .Include(c => c.Locataire)
                .Include(c => c.Paiements)
                .Where(c => c.Statut == StatutContrat.Actif)
                .ToListAsync();

            foreach (var contrat in contratsActifs)
            {
                var joursRestantsBail = (contrat.DateFin.Date - maintenant.Date).Days;

                if (joursRestantsBail <= 30)
                {
                    string messageBail;
                    string severiteBail;

                    if (joursRestantsBail < 0)
                    {
                        messageBail = $"{TextesMessages.ContratDuBien} {contrat.Bien?.Reference} ({TextesMessages.Locataire} {contrat.Locataire?.Nom}) {TextesMessages.AExpireIlYa} {Math.Abs(joursRestantsBail)} {TextesMessages.Jours}";
                        severiteBail = "danger";
                    }
                    else if (joursRestantsBail == 0)
                    {
                        messageBail = $"{TextesMessages.ContratDuBien} {contrat.Bien?.Reference} ({TextesMessages.Locataire} {contrat.Locataire?.Nom}) {TextesMessages.ExpireAujourdhui}";
                        severiteBail = "danger";
                    }
                    else if (joursRestantsBail == 1)
                    {
                        messageBail = $"{TextesMessages.ContratDuBien} {contrat.Bien?.Reference} ({TextesMessages.Locataire} {contrat.Locataire?.Nom}) {TextesMessages.ExpireDemain}";
                        severiteBail = "danger";
                    }
                    else
                    {
                        messageBail = $"{TextesMessages.ContratDuBien} {contrat.Bien?.Reference} {TextesMessages.ExpireDans} {joursRestantsBail} {TextesMessages.Jours_}";
                        severiteBail = "warn";
                    }

                    alertesList.Add(new
                    {
                        Id = $"bail-{contrat.Id}",
                        Titre = TextesMessages.FinImminenteContrat,
                        Message = messageBail,
                        Severite = severiteBail
                    });
                }

                int delaiToleranceJours = contrat.DelaiJoursTolerance;
                var premierJourMois = new DateTime(maintenant.Year, maintenant.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var dateLimitePaiementMois = premierJourMois.AddDays(delaiToleranceJours);

                if (contrat.DateDebut <= maintenant && maintenant > dateLimitePaiementMois)
                {
                    bool aPayeCeMois = contrat.Paiements != null && contrat.Paiements.Any(p =>
                        p.StatutTransaction == StatutTransaction.Confirme &&
                        p.DatePaiement.Month == maintenant.Month &&
                        p.DatePaiement.Year == maintenant.Year);

                    if (!aPayeCeMois)
                    {
                        var joursDeRetard = (maintenant.Date - dateLimitePaiementMois.Date).Days;

                        alertesList.Add(new
                        {
                            Id = $"impaye-{contrat.Id}-{maintenant.Month}",
                            Titre = TextesMessages.RetardPaiement,
                            Message = $"{TextesMessages.LoyerDuBien} {contrat.Bien?.Reference} ({TextesMessages.Locataire} {contrat.Locataire?.Nom}) est en retard de {joursDeRetard} jour(s) (Délai de tolérance : {delaiToleranceJours}j).",
                            Severite = joursDeRetard > 10 ? "danger" : "warn"
                        });
                    }
                }
            }

            return Ok(new
            {
                TotalBiens = totalBiens,
                BiensDisponibles = totalBiens - biensLoues,
                TauxOccupation = Math.Round(tauxOccupation, 2),
                EncaissementsMois = encaissementsMois,
                Alertes = alertesList,
                EncaissementsGraph = new
                {
                    Labels = labels,
                    Datasets = datasets
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Erreur interne KPI : {ex.Message} - {ex.InnerException?.Message}" });
        }
    }

    // ==========================================
    // ENDPOINTS POUR LES RAPPORTS PDF & LISTES
    // ==========================================

    [HttpGet("rapports/encaissements-par-mode")]
    public async Task<IActionResult> GetEncaissementsParMode([FromQuery] DateTime? dateDebut, [FromQuery] DateTime? dateFin)
    {
        var query = _context.Paiements
            .Include(p => p.Contrat)
                .ThenInclude(c => c.Bien)
            .Include(p => p.Contrat)
                .ThenInclude(c => c.Locataire)
            .Where(p => p.StatutTransaction == StatutTransaction.Confirme);

        if (dateDebut.HasValue)
            query = query.Where(p => p.DatePaiement >= dateDebut.Value);

        if (dateFin.HasValue)
            query = query.Where(p => p.DatePaiement <= dateFin.Value);

        var paiements = await query
            .OrderByDescending(p => p.DatePaiement)
            .Select(p => new
            {
                p.Id,
                p.DatePaiement,
                p.Montant,
                ModePaiement = p.ModePaiement.ToString(),
                BienReference = p.Contrat != null && p.Contrat.Bien != null ? p.Contrat.Bien.Reference : TextesMessages.NonApplique,
                LocataireNom = p.Contrat != null && p.Contrat.Locataire != null ? p.Contrat.Locataire.Nom : TextesMessages.NonApplique
            })
            .ToListAsync();

        var groupes = paiements
            .GroupBy(p => p.ModePaiement)
            .Select(g => new
            {
                ModePaiement = g.Key,
                TotalRecette = g.Sum(x => x.Montant),
                NombreTransactions = g.Count(),
                Details = g.ToList()
            });

        return Ok(groupes);
    }

    [HttpGet("rapports/historique-locataire/{locataireId}")]
    public async Task<IActionResult> GetHistoriquePaiementsLocataire(Guid locataireId)
    {
        var locataire = await _context.Utilisateurs.FindAsync(locataireId);
        if (locataire == null)
            return NotFound(TextesMessages.LocataireIntrouvable);

        var paiements = await _context.Paiements
            .Include(p => p.Contrat)
                .ThenInclude(c => c.Bien)
            .Where(p => p.Contrat != null && p.Contrat.LocataireId == locataireId)
            .OrderByDescending(p => p.DatePaiement)
            .Select(p => new
            {
                p.Id,
                p.DatePaiement,
                p.Montant,
                Statut = p.StatutTransaction.ToString(),
                ModePaiement = p.ModePaiement.ToString(),
                BienReference = p.Contrat != null && p.Contrat.Bien != null ? p.Contrat.Bien.Reference : "N/A"
            })
            .ToListAsync();

        return Ok(new
        {
            Locataire = new { locataire.Id, locataire.Prenom, locataire.Nom, locataire.Email, locataire.Telephone },
            TotalVersements = paiements.Where(p => p.Statut == StatutTransaction.Confirme.ToString()).Sum(p => p.Montant),
            Historique = paiements
        });
    }

    [HttpGet("rapports/contrats")]
    public async Task<IActionResult> GetRapportContrats([FromQuery] StatutContrat? statut)
    {
        var query = _context.Contrats
            .Include(c => c.Bien)
            .Include(c => c.Locataire)
            .AsQueryable();

        if (statut.HasValue)
            query = query.Where(c => c.Statut == statut.Value);

        var contrats = await query
            .OrderByDescending(c => c.DateDebut)
            .Select(c => new
            {
                c.Id,
                BienReference = c.Bien != null ? c.Bien.Reference : TextesMessages.NonApplique,
                LocataireNom = c.Locataire != null ? c.Locataire.Nom : TextesMessages.NonApplique,
                c.DateDebut,
                c.DateFin,
                c.MontantLoyer,
                c.MontantCaution,
                StatutContrat = c.Statut.ToString(),
                c.DelaiJoursTolerance
            })
            .ToListAsync();

        return Ok(contrats);
    }

    [HttpPost("export-pdf")]
    public IActionResult ExporterRapportPdf([FromQuery] string typeRapport, [FromQuery] string titreRapport, [FromBody] JsonElement donnees)
    {
        try
        {
            byte[] pdfBytes = _rapportPdfService.GenererRapportPdf(typeRapport, titreRapport, donnees);
            return File(pdfBytes, "application/pdf", $"Rapport_{typeRapport}_{DateTime.Now:yyyyMMdd}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Erreur lors de la génération du PDF : {ex.Message}");
        }
    }

    [HttpGet("rapports/impayes")]
    public async Task<IActionResult> GetRapportImpayes()
    {
        try
        {
            var maintenant = DateTime.UtcNow;
            var contratsActifs = await _context.Contrats
                .Include(c => c.Bien)
                .Include(c => c.Locataire)
                .Include(c => c.Paiements)
                .Where(c => c.Statut == StatutContrat.Actif)
                .ToListAsync();

            var impayesList = new List<object>();

            foreach (var contrat in contratsActifs)
            {
                int delaiToleranceJours = contrat.DelaiJoursTolerance;
                var premierJourMois = new DateTime(maintenant.Year, maintenant.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var dateLimitePaiementMois = premierJourMois.AddDays(delaiToleranceJours);

                if (contrat.DateDebut <= maintenant && maintenant > dateLimitePaiementMois)
                {
                    bool aPayeCeMois = contrat.Paiements != null && contrat.Paiements.Any(p =>
                        p.StatutTransaction == StatutTransaction.Confirme &&
                        p.DatePaiement.Month == maintenant.Month &&
                        p.DatePaiement.Year == maintenant.Year);

                    if (!aPayeCeMois)
                    {
                        var joursDeRetard = (maintenant.Date - dateLimitePaiementMois.Date).Days;

                        impayesList.Add(new
                        {
                            ContratId = contrat.Id,
                            BienReference = contrat.Bien?.Reference ?? "N/A",
                            LocataireNom = contrat.Locataire?.Nom ?? "N/A",
                            LocataireTelephone = contrat.Locataire?.Telephone ?? "N/A",
                            MontantLoyer = contrat.MontantLoyer,
                            DelaiToleranceJours = delaiToleranceJours,
                            DateLimite = dateLimitePaiementMois,
                            JoursDeRetard = joursDeRetard
                        });
                    }
                }
            }

            return Ok(impayesList);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Erreur impayés : {ex.Message} - {ex.InnerException?.Message}" });
        }
    }
}