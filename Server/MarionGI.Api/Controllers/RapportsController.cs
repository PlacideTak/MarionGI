using MarionGI.Domain.Enums;
using MarionGI.Infrastructure.Pdf;
using MarionGI.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

namespace MarionGI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(
    Roles =
        TextesMessages.DroitAdministrateur + "," +
        TextesMessages.DroitGestionnaire + "," +
        TextesMessages.DroitProprietaire)]
public class RapportsController : ControllerBase
{
    private readonly MarionDbContext _context;
    private readonly IRapportPdfService _rapportPdfService;

    public RapportsController(
        MarionDbContext context,
        IRapportPdfService rapportPdfService)
    {
        _context = context;
        _rapportPdfService = rapportPdfService;
    }

    // ============================================================
    // 1. DASHBOARD / KPI
    // ============================================================

    [HttpGet("dashboard-kpi")]
    public async Task<IActionResult> GetKpis()
    {
        try
        {
            var societeId =
                await GetSocieteIdUtilisateurConnecteAsync();

            if (!societeId.HasValue)
            {
                return Forbid();
            }

            var maintenant = DateTime.UtcNow;

            var debutMois = new DateTime(
                maintenant.Year,
                maintenant.Month,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc);

            var debutMoisSuivant =
                debutMois.AddMonths(1);

            // ========================================================
            // 1. BIENS
            // ========================================================

            var totalBiens =
                await _context.BiensImmobiliers
                    .AsNoTracking()
                    .Where(b =>
                        !b.EstSupprime &&
                        b.SocieteId == societeId.Value)
                    .CountAsync();

            // ========================================================
            // 2. UNITÉS
            // ========================================================

            var totalUnites =
                await _context.UnitesLocatives
                    .AsNoTracking()
                    .Where(u =>
                        !u.EstSupprime &&
                        u.BienImmobilier != null &&
                        !u.BienImmobilier.EstSupprime &&
                        u.BienImmobilier.SocieteId ==
                            societeId.Value)
                    .CountAsync();

            var unitesLouees =
                await _context.UnitesLocatives
                    .AsNoTracking()
                    .Where(u =>
                        !u.EstSupprime &&
                        u.Statut ==
                            StatutDisponibilite.Loue &&
                        u.BienImmobilier != null &&
                        !u.BienImmobilier.EstSupprime &&
                        u.BienImmobilier.SocieteId ==
                            societeId.Value)
                    .CountAsync();

            var unitesDisponibles =
                await _context.UnitesLocatives
                    .AsNoTracking()
                    .Where(u =>
                        !u.EstSupprime &&
                        u.Statut ==
                            StatutDisponibilite.Disponible &&
                        u.BienImmobilier != null &&
                        !u.BienImmobilier.EstSupprime &&
                        u.BienImmobilier.SocieteId ==
                            societeId.Value)
                    .CountAsync();

            var tauxOccupation =
                totalUnites > 0
                    ? (double)unitesLouees /
                      totalUnites * 100
                    : 0;

            // ========================================================
            // 3. ENCAISSEMENTS DU MOIS
            // ========================================================

            var encaissementsMois =
                await _context.Paiements
                    .AsNoTracking()
                    .Where(p =>
                        p.StatutTransaction ==
                            StatutTransaction.Confirme &&
                        p.Contrat != null &&
                        p.Contrat.UniteLocative != null &&
                        p.Contrat.UniteLocative.BienImmobilier != null &&
                        p.Contrat.UniteLocative.BienImmobilier.SocieteId ==
                            societeId.Value &&
                        p.DatePaiement >= debutMois &&
                        p.DatePaiement < debutMoisSuivant)
                    .SumAsync(p => (decimal?)p.Montant)
                    ?? 0m;

            // ========================================================
            // 4. ÉVOLUTION DES ENCAISSEMENTS SUR 12 MOIS
            // ========================================================

            var labels = new List<string>();
            var datasets = new List<decimal>();

            for (var i = 11; i >= 0; i--)
            {
                var mois =
                    debutMois.AddMonths(-i);

                var moisSuivant =
                    mois.AddMonths(1);

                labels.Add(
                    mois.ToString(
                        "MMM",
                        new CultureInfo("fr-FR")));

                var totalMois =
                    await _context.Paiements
                        .AsNoTracking()
                        .Where(p =>
                            p.StatutTransaction ==
                                StatutTransaction.Confirme &&
                            p.Contrat != null &&
                            p.Contrat.UniteLocative != null &&
                            p.Contrat.UniteLocative.BienImmobilier != null &&
                            p.Contrat.UniteLocative.BienImmobilier.SocieteId ==
                                societeId.Value &&
                            p.DatePaiement >= mois &&
                            p.DatePaiement < moisSuivant)
                        .SumAsync(p => (decimal?)p.Montant)
                        ?? 0m;

                datasets.Add(totalMois);
            }

            // ========================================================
            // 5. CONTRATS
            // ========================================================
            //
            // On charge également les paiements car ils sont
            // nécessaires au calcul réel des impayés.
            // ========================================================

            var contrats =
                await _context.Contrats
                    .AsNoTracking()
                    .Include(c => c.UniteLocative)
                        .ThenInclude(u => u.BienImmobilier)
                    .Include(c => c.Locataire)
                    .Include(c => c.Paiements)
                    .Where(c =>
                        !c.EstSupprime &&
                        c.UniteLocative != null &&
                        c.UniteLocative.BienImmobilier != null &&
                        c.UniteLocative.BienImmobilier.SocieteId ==
                            societeId.Value)
                    .ToListAsync();

            // ========================================================
            // 6. ALERTES
            // ========================================================

            var alertes = new List<object>();

            // ========================================================
            // 6.1 CONTRATS EXPIRÉS
            // ========================================================

            var contratsExpires =
                contrats
                    .Where(c =>
                        c.DateFin < maintenant)
                    .OrderBy(c => c.DateFin)
                    .ToList();

            foreach (var contrat in contratsExpires)
            {
                var joursDepuisExpiration =
                    (maintenant.Date -
                     contrat.DateFin.Date).Days;

                var uniteReference =
                    contrat.UniteLocative?.Reference
                    ?? TextesMessages.NonApplique;

                var bienNom =
                    contrat.UniteLocative?
                        .BienImmobilier?
                        .Nom
                    ?? TextesMessages.NonApplique;

                var locataireNom =
                    contrat.Locataire == null
                        ? TextesMessages.NonApplique
                        : $"{contrat.Locataire.Prenom} " +
                          $"{contrat.Locataire.Nom}";

                alertes.Add(new
                {
                    Id = contrat.Id,

                    Titre = "Contrat expiré",

                    Message =
                        $"Le contrat de l'unité " +
                        $"{uniteReference} de l'immeuble " +
                        $"{bienNom} est expiré depuis " +
                        $"{joursDepuisExpiration} jour(s). " +
                        $"Locataire : {locataireNom}.",

                    Severite = "danger",

                    Type = "ContratExpire",

                    ContratId = contrat.Id,

                    UniteReference = uniteReference,

                    BienNom = bienNom,

                    DateFin = contrat.DateFin,

                    JoursDepuisExpiration =
                        joursDepuisExpiration
                });
            }

            // ========================================================
            // 6.2 CONTRATS BIENTÔT EXPIRÉS
            // ========================================================

            var dateLimiteAlerte =
                maintenant.AddDays(30);

            var contratsBientotExpires =
                contrats
                    .Where(c =>
                        c.DateFin >= maintenant &&
                        c.DateFin <= dateLimiteAlerte)
                    .OrderBy(c => c.DateFin)
                    .ToList();

            foreach (var contrat in contratsBientotExpires)
            {
                var joursAvantExpiration =
                    (contrat.DateFin.Date -
                     maintenant.Date).Days;

                var uniteReference =
                    contrat.UniteLocative?.Reference
                    ?? TextesMessages.NonApplique;

                var bienNom =
                    contrat.UniteLocative?
                        .BienImmobilier?
                        .Nom
                    ?? TextesMessages.NonApplique;

                var locataireNom =
                    contrat.Locataire == null
                        ? TextesMessages.NonApplique
                        : $"{contrat.Locataire.Prenom} " +
                          $"{contrat.Locataire.Nom}";

                alertes.Add(new
                {
                    Id = contrat.Id,

                    Titre = "Contrat bientôt expiré",

                    Message =
                        $"Le contrat de l'unité " +
                        $"{uniteReference} de l'immeuble " +
                        $"{bienNom} expire dans " +
                        $"{joursAvantExpiration} jour(s). " +
                        $"Locataire : {locataireNom}.",

                    Severite =
                        joursAvantExpiration <= 7
                            ? "warning"
                            : "info",

                    Type = "ContratBientotExpire",

                    ContratId = contrat.Id,

                    UniteReference = uniteReference,

                    BienNom = bienNom,

                    DateFin = contrat.DateFin,

                    JoursAvantExpiration =
                        joursAvantExpiration
                });
            }

            // ========================================================
            // 7. IMPAYÉS DU MOIS
            // ========================================================
            //
            // Un paiement partiel ne suffit plus à considérer le
            // loyer comme entièrement payé.
            // ========================================================

            var contratsActifs =
                contrats
                    .Where(c =>
                        c.Statut ==
                            StatutContrat.Actif &&
                        c.DateDebut <= maintenant &&
                        c.DateFin >= maintenant)
                    .ToList();

            var impayes =
                new List<object>();

            foreach (var contrat in contratsActifs)
            {
                var dateLimitePaiement =
                    debutMois.AddDays(
                        contrat.DelaiJoursTolerance);

                if (maintenant <= dateLimitePaiement)
                {
                    continue;
                }

                var montantPaye =
                    contrat.Paiements?
                        .Where(p =>
                            p.StatutTransaction ==
                                StatutTransaction.Confirme &&
                            p.DatePaiement >= debutMois &&
                            p.DatePaiement < debutMoisSuivant)
                        .Sum(p => p.Montant)
                    ?? 0m;

                var montantDu =
                    contrat.MontantLoyer;

                var solde =
                    montantDu - montantPaye;

                if (solde <= 0)
                {
                    continue;
                }

                var joursRetard =
                    (maintenant.Date -
                     dateLimitePaiement.Date).Days;

                var uniteReference =
                    contrat.UniteLocative?.Reference
                    ?? TextesMessages.NonApplique;

                var bienNom =
                    contrat.UniteLocative?
                        .BienImmobilier?
                        .Nom
                    ?? TextesMessages.NonApplique;

                var locataireNom =
                    contrat.Locataire == null
                        ? TextesMessages.NonApplique
                        : $"{contrat.Locataire.Prenom} " +
                          $"{contrat.Locataire.Nom}";

                var impaye = new
                {
                    Id = contrat.Id,

                    Titre =
                        montantPaye > 0
                            ? "Loyer partiellement payé"
                            : "Loyer en retard",

                    Message =
                        $"Le loyer de l'unité " +
                        $"{uniteReference} de l'immeuble " +
                        $"{bienNom} présente un solde de " +
                        $"{solde:N0} FCFA. " +
                        $"Retard : {joursRetard} jour(s). " +
                        $"Locataire : {locataireNom}.",

                    Severite = "danger",

                    Type = "Impayé",

                    ContratId = contrat.Id,

                    UniteReference = uniteReference,

                    BienNom = bienNom,

                    MontantLoyer =
                        montantDu,

                    MontantPaye =
                        montantPaye,

                    Solde =
                        solde,

                    DateLimite =
                        dateLimitePaiement,

                    JoursDeRetard =
                        joursRetard
                };

                impayes.Add(impaye);

                alertes.Add(impaye);
            }

            // ========================================================
            // 8. TRI DES ALERTES
            // ========================================================

            alertes =
                alertes
                    .OrderByDescending(a =>
                    {
                        var severite =
                            a.GetType()
                                .GetProperty("Severite")
                                ?.GetValue(a)
                                ?.ToString();

                        return severite switch
                        {
                            "danger" => 3,
                            "warning" => 2,
                            _ => 1
                        };
                    })
                    .ToList();

            // ========================================================
            // 9. DERNIÈRES UNITÉS
            // ========================================================

            var dernieresUnites =
                await _context.UnitesLocatives
                    .AsNoTracking()
                    .Where(u =>
                        !u.EstSupprime &&
                        u.BienImmobilier != null &&
                        !u.BienImmobilier.EstSupprime &&
                        u.BienImmobilier.SocieteId ==
                            societeId.Value)
                    .OrderByDescending(u => u.DateCreation)
                    .Take(5)
                    .Select(u => new
                    {
                        u.Id,
                        u.Reference,
                        u.Type,
                        u.Superficie,
                        u.Loyer,
                        u.Statut,
                        u.DateCreation,

                        Bien = new
                        {
                            u.BienImmobilier!.Id,
                            u.BienImmobilier.Reference,
                            u.BienImmobilier.Nom
                        }
                    })
                    .ToListAsync();

            // ========================================================
            // 10. STATISTIQUES CONTRATS
            // ========================================================

            var contratsActifsCount =
                contrats.Count(c =>
                    c.Statut ==
                        StatutContrat.Actif &&
                    c.DateDebut <= maintenant &&
                    c.DateFin >= maintenant);

            var contratsExpiresCount =
                contrats.Count(c =>
                    c.DateFin < maintenant);

            // ========================================================
            // 11. RÉPONSE
            // ========================================================

            return Ok(new
            {
                TotalBiens = totalBiens,

                TotalUnites = totalUnites,

                UnitesLouees = unitesLouees,

                UnitesDisponibles = unitesDisponibles,

                TauxOccupation =
                    Math.Round(
                        tauxOccupation,
                        2),

                EncaissementsMois =
                    encaissementsMois,

                EncaissementsGraph = new
                {
                    Labels = labels,
                    Datasets = datasets
                },

                ContratsActifs =
                    contratsActifsCount,

                ContratsExpires =
                    contratsExpiresCount,

                NombreImpayes =
                    impayes.Count,

                MontantImpayes =
                    impayes.Sum(i =>
                    {
                        var property =
                            i.GetType()
                                .GetProperty("Solde");

                        return property?.GetValue(i) is decimal value
                            ? value
                            : 0m;
                    }),

                Alertes = alertes,

                Impayes = impayes,

                DernieresUnites = dernieresUnites
            });
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new
                {
                    message =
                        "Erreur interne lors du chargement des KPI.",

                    detail =
                        ex.InnerException?.Message
                        ?? ex.Message
                });
        }
    }

    // ============================================================
    // 2. RAPPORT D'UNE SOCIÉTÉ
    // ============================================================

    [HttpGet("societe/{societeId:guid}")]
    public async Task<IActionResult> GetRapportSociete(
        Guid societeId)
    {
        var societeUtilisateur =
            await GetSocieteIdUtilisateurConnecteAsync();

        if (!societeUtilisateur.HasValue)
        {
            return Forbid();
        }

        if (societeUtilisateur.Value != societeId)
        {
            return Forbid();
        }

        var societe =
            await _context.Societes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.Id == societeId);

        if (societe == null)
        {
            return NotFound(
                $"La société {societeId} est introuvable.");
        }

        var biens =
            await _context.BiensImmobiliers
                .AsNoTracking()
                .Include(b => b.UnitesLocatives)
                .Where(b =>
                    b.SocieteId == societeId &&
                    !b.EstSupprime)
                .ToListAsync();

        var uniteIds =
            biens
                .SelectMany(b => b.UnitesLocatives)
                .Where(u => !u.EstSupprime)
                .Select(u => u.Id)
                .ToList();

        var contrats =
            await _context.Contrats
                .AsNoTracking()
                .Include(c => c.UniteLocative)
                .Include(c => c.Locataire)
                .Include(c => c.Paiements)
                .Where(c =>
                    !c.EstSupprime &&
                    uniteIds.Contains(
                        c.UniteLocativeId))
                .ToListAsync();

        var paiements =
            contrats
                .SelectMany(c => c.Paiements ?? [])
                .Where(p =>
                    p.StatutTransaction ==
                        StatutTransaction.Confirme)
                .ToList();

        var unites =
            biens
                .SelectMany(b => b.UnitesLocatives)
                .Where(u => !u.EstSupprime)
                .ToList();

        var totalUnites =
            unites.Count;

        var unitesLouees =
            unites.Count(u =>
                u.Statut ==
                    StatutDisponibilite.Loue);

        var unitesDisponibles =
            unites.Count(u =>
                u.Statut ==
                    StatutDisponibilite.Disponible);

        var tauxOccupation =
            totalUnites > 0
                ? (double)unitesLouees /
                  totalUnites * 100
                : 0;

        var maintenant =
            DateTime.UtcNow;

        var debutMois =
            new DateTime(
                maintenant.Year,
                maintenant.Month,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc);

        var debutMoisSuivant =
            debutMois.AddMonths(1);

        var encaissementsMois =
            paiements
                .Where(p =>
                    p.DatePaiement >= debutMois &&
                    p.DatePaiement < debutMoisSuivant)
                .Sum(p => p.Montant);

        var contratsActifs =
            contrats
                .Where(c =>
                    c.Statut ==
                        StatutContrat.Actif &&
                    c.DateDebut <= maintenant &&
                    c.DateFin >= maintenant)
                .ToList();

        var impayes =
            await ConstruireImpayesAsync(
                contratsActifs,
                debutMois,
                debutMoisSuivant,
                maintenant);

        return Ok(new
        {
            Societe = new
            {
                societe.Id,
                societe.Nom,
                societe.NumeroEntreprise,
                societe.Adresse,
                societe.Ville,
                societe.CodePostal,
                societe.Telephone,
                societe.Email
            },

            Statistiques = new
            {
                NombreBiens =
                    biens.Count,

                NombreUnites =
                    totalUnites,

                UnitesLouees =
                    unitesLouees,

                UnitesDisponibles =
                    unitesDisponibles,

                TauxOccupation =
                    Math.Round(
                        tauxOccupation,
                        2),

                NombreContratsActifs =
                    contratsActifs.Count,

                NombreImpayes =
                    impayes.Count,

                MontantImpayes =
                    impayes.Sum(i =>
                        i.Solde),

                EncaissementsMois =
                    encaissementsMois
            },

            Biens =
                biens.Select(b =>
                {
                    var unitesBien =
                        b.UnitesLocatives
                            .Where(u =>
                                !u.EstSupprime)
                            .ToList();

                    return new
                    {
                        b.Id,
                        b.Reference,

                        // Nom de baptême de l'immeuble
                        b.Nom,

                        b.Type,
                        b.Adresse,
                        b.Ville,
                        b.Quartier,
                        b.Superficie,

                        NombreUnites =
                            unitesBien.Count,

                        UnitesLouees =
                            unitesBien.Count(u =>
                                u.Statut ==
                                StatutDisponibilite.Loue),

                        UnitesDisponibles =
                            unitesBien.Count(u =>
                                u.Statut ==
                                StatutDisponibilite.Disponible)
                    };
                })
                .ToList(),

            Impayes = impayes
        });
    }

    // ============================================================
    // 3. RAPPORT D'UN BIEN IMMOBILIER
    // ============================================================

    [HttpGet("bien/{bienImmobilierId:guid}")]
    public async Task<IActionResult> GetRapportBien(
        Guid bienImmobilierId)
    {
        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync();

        if (!societeId.HasValue)
        {
            return Forbid();
        }

        var bien =
            await _context.BiensImmobiliers
                .AsNoTracking()
                .Include(b => b.Societe)
                .Include(b => b.UnitesLocatives)
                .FirstOrDefaultAsync(b =>
                    b.Id == bienImmobilierId &&
                    !b.EstSupprime &&
                    b.SocieteId ==
                        societeId.Value);

        if (bien == null)
        {
            return NotFound(
                $"Le bien immobilier {bienImmobilierId} " +
                $"est introuvable.");
        }

        var unites =
            bien.UnitesLocatives
                .Where(u => !u.EstSupprime)
                .ToList();

        var uniteIds =
            unites
                .Select(u => u.Id)
                .ToList();

        var contrats =
            await _context.Contrats
                .AsNoTracking()
                .Include(c => c.UniteLocative)
                .Include(c => c.Locataire)
                .Include(c => c.Paiements)
                .Where(c =>
                    !c.EstSupprime &&
                    uniteIds.Contains(
                        c.UniteLocativeId))
                .ToListAsync();

        var paiements =
            contrats
                .SelectMany(c => c.Paiements ?? [])
                .Where(p =>
                    p.StatutTransaction ==
                        StatutTransaction.Confirme)
                .ToList();

        var totalUnites =
            unites.Count;

        var unitesLouees =
            unites.Count(u =>
                u.Statut ==
                    StatutDisponibilite.Loue);

        var unitesDisponibles =
            unites.Count(u =>
                u.Statut ==
                    StatutDisponibilite.Disponible);

        var tauxOccupation =
            totalUnites > 0
                ? (double)unitesLouees /
                  totalUnites * 100
                : 0;

        var maintenant =
            DateTime.UtcNow;

        var debutMois =
            new DateTime(
                maintenant.Year,
                maintenant.Month,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc);

        var debutMoisSuivant =
            debutMois.AddMonths(1);

        var encaissementsMois =
            paiements
                .Where(p =>
                    p.DatePaiement >= debutMois &&
                    p.DatePaiement < debutMoisSuivant)
                .Sum(p => p.Montant);

        var loyersMensuels =
            unites
                .Where(u =>
                    u.Statut ==
                        StatutDisponibilite.Loue)
                .Sum(u => u.Loyer);

        var contratsActifs =
            contrats
                .Where(c =>
                    c.Statut ==
                        StatutContrat.Actif &&
                    c.DateDebut <= maintenant &&
                    c.DateFin >= maintenant)
                .ToList();

        return Ok(new
        {
            Societe =
                bien.Societe == null
                    ? null
                    : new
                    {
                        bien.Societe.Id,
                        bien.Societe.Nom
                    },

            Bien = new
            {
                bien.Id,
                bien.Reference,

                // Nom de l'immeuble
                bien.Nom,

                bien.Type,
                bien.Adresse,
                bien.Ville,
                bien.Quartier,
                bien.Superficie
            },

            Statistiques = new
            {
                NombreUnites =
                    totalUnites,

                UnitesLouees =
                    unitesLouees,

                UnitesDisponibles =
                    unitesDisponibles,

                TauxOccupation =
                    Math.Round(
                        tauxOccupation,
                        2),

                LoyersMensuels =
                    loyersMensuels,

                EncaissementsMois =
                    encaissementsMois,

                ContratsActifs =
                    contratsActifs.Count
            },

            Unites =
                unites.Select(u =>
                {
                    var contratActif =
                        contrats.FirstOrDefault(c =>
                            c.UniteLocativeId ==
                                u.Id &&
                            c.Statut ==
                                StatutContrat.Actif &&
                            c.DateDebut <= maintenant &&
                            c.DateFin >= maintenant);

                    return new
                    {
                        u.Id,
                        u.Reference,
                        u.Type,
                        u.Superficie,
                        u.Loyer,
                        u.Statut,

                        ContratActif =
                            contratActif == null
                                ? null
                                : new
                                {
                                    contratActif.Id,
                                    contratActif.DateDebut,
                                    contratActif.DateFin,
                                    contratActif.MontantLoyer,

                                    Locataire =
                                        contratActif.Locataire == null
                                            ? null
                                            : new
                                            {
                                                contratActif.Locataire.Id,
                                                contratActif.Locataire.Nom,
                                                contratActif.Locataire.Prenom,
                                                contratActif.Locataire.Telephone
                                            }
                                }
                    };
                })
                .ToList()
        });
    }

    // ============================================================
    // 4. RAPPORT D'UNE UNITÉ LOCATIVE
    // ============================================================

    [HttpGet("unite/{uniteLocativeId:guid}")]
    public async Task<IActionResult> GetRapportUnite(
        Guid uniteLocativeId)
    {
        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync();

        if (!societeId.HasValue)
        {
            return Forbid();
        }

        var unite =
            await _context.UnitesLocatives
                .AsNoTracking()
                .Include(u => u.BienImmobilier)
                    .ThenInclude(b => b.Societe)
                .FirstOrDefaultAsync(u =>
                    u.Id == uniteLocativeId &&
                    !u.EstSupprime &&
                    u.BienImmobilier != null &&
                    !u.BienImmobilier.EstSupprime &&
                    u.BienImmobilier.SocieteId ==
                        societeId.Value);

        if (unite == null)
        {
            return NotFound(
                $"L'unité locative {uniteLocativeId} " +
                $"est introuvable.");
        }

        var contrats =
            await _context.Contrats
                .AsNoTracking()
                .Include(c => c.Locataire)
                .Include(c => c.Paiements)
                .Where(c =>
                    c.UniteLocativeId ==
                        uniteLocativeId &&
                    !c.EstSupprime)
                .OrderByDescending(c =>
                    c.DateDebut)
                .ToListAsync();

        var maintenant =
            DateTime.UtcNow;

        var contratActif =
            contrats.FirstOrDefault(c =>
                c.Statut ==
                    StatutContrat.Actif &&
                c.DateDebut <= maintenant &&
                c.DateFin >= maintenant);

        var paiements =
            contrats
                .SelectMany(c => c.Paiements ?? [])
                .Where(p =>
                    p.StatutTransaction ==
                        StatutTransaction.Confirme)
                .OrderByDescending(p =>
                    p.DatePaiement)
                .ToList();

        var totalPaye =
            paiements.Sum(p => p.Montant);

        var debutMois =
            new DateTime(
                maintenant.Year,
                maintenant.Month,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc);

        var debutMoisSuivant =
            debutMois.AddMonths(1);

        var paiementMois =
            paiements
                .Where(p =>
                    p.DatePaiement >= debutMois &&
                    p.DatePaiement < debutMoisSuivant)
                .Sum(p => p.Montant);

        var montantLoyerActuel =
            contratActif?.MontantLoyer
            ?? unite.Loyer;

        var impayeMois =
            contratActif == null
                ? 0m
                : Math.Max(
                    0m,
                    montantLoyerActuel -
                    paiementMois);

        return Ok(new
        {
            Societe =
                unite.BienImmobilier?.Societe == null
                    ? null
                    : new
                    {
                        unite.BienImmobilier.Societe.Id,
                        unite.BienImmobilier.Societe.Nom
                    },

            BienImmobilier =
                unite.BienImmobilier == null
                    ? null
                    : new
                    {
                        unite.BienImmobilier.Id,
                        unite.BienImmobilier.Reference,
                        unite.BienImmobilier.Nom,
                        unite.BienImmobilier.Type,
                        unite.BienImmobilier.Adresse,
                        unite.BienImmobilier.Ville,
                        unite.BienImmobilier.Quartier
                    },

            Unite = new
            {
                unite.Id,
                unite.Reference,
                unite.Type,
                unite.Superficie,
                unite.Loyer,
                unite.Statut
            },

            ContratActif =
                contratActif == null
                    ? null
                    : new
                    {
                        contratActif.Id,
                        contratActif.DateDebut,
                        contratActif.DateFin,
                        contratActif.MontantLoyer,
                        contratActif.MontantCaution,
                        contratActif.FrequencePaiement,
                        contratActif.DelaiJoursTolerance,

                        Locataire =
                            contratActif.Locataire == null
                                ? null
                                : new
                                {
                                    contratActif.Locataire.Id,
                                    contratActif.Locataire.Nom,
                                    contratActif.Locataire.Prenom,
                                    contratActif.Locataire.Email,
                                    contratActif.Locataire.Telephone
                                }
                    },

            Statistiques = new
            {
                NombreContrats =
                    contrats.Count,

                TotalPaye =
                    totalPaye,

                PaiementMois =
                    paiementMois,

                LoyerDuMois =
                    montantLoyerActuel,

                SoldeImpayéMois =
                    impayeMois,

                NombrePaiements =
                    paiements.Count
            },

            HistoriquePaiements =
                paiements.Select(p => new
                {
                    p.Id,
                    p.DatePaiement,
                    p.Montant,

                    ModePaiement =
                        p.ModePaiement.ToString(),

                    Statut =
                        p.StatutTransaction.ToString()
                })
        });
    }

    // ============================================================
    // 5. ENCAISSEMENTS PAR MODE
    // ============================================================

    [HttpGet("encaissements-par-mode")]
    public async Task<IActionResult> GetEncaissementsParMode(
        [FromQuery] DateTime? dateDebut,
        [FromQuery] DateTime? dateFin)
    {
        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync();

        if (!societeId.HasValue)
        {
            return Forbid();
        }

        if (dateDebut.HasValue &&
            dateFin.HasValue &&
            dateDebut.Value.Date >
            dateFin.Value.Date)
        {
            return BadRequest(
                "La date de début doit être antérieure " +
                "ou égale à la date de fin.");
        }

        var query =
            _context.Paiements
                .AsNoTracking()
                .Where(p =>
                    p.StatutTransaction ==
                        StatutTransaction.Confirme &&
                    p.Contrat != null &&
                    p.Contrat.UniteLocative != null &&
                    p.Contrat.UniteLocative.BienImmobilier != null &&
                    p.Contrat.UniteLocative.BienImmobilier.SocieteId ==
                        societeId.Value);

        if (dateDebut.HasValue)
        {
            var debut =
                dateDebut.Value.Date;

            query = query.Where(
                p => p.DatePaiement >= debut);
        }

        if (dateFin.HasValue)
        {
            // La date de fin est incluse dans toute la journée.
            var finExclusive =
                dateFin.Value.Date.AddDays(1);

            query = query.Where(
                p => p.DatePaiement < finExclusive);
        }

        var paiements =
            await query
                .OrderByDescending(p =>
                    p.DatePaiement)
                .Select(p => new
                {
                    p.Id,
                    p.DatePaiement,
                    p.Montant,

                    ModePaiement =
                        p.ModePaiement.ToString(),

                    UniteReference =
                        p.Contrat!
                            .UniteLocative!
                            .Reference,

                    BienNom =
                        p.Contrat!
                            .UniteLocative!
                            .BienImmobilier!
                            .Nom,

                    BienReference =
                        p.Contrat!
                            .UniteLocative!
                            .BienImmobilier!
                            .Reference,

                    LocataireNom =
                        p.Contrat!.Locataire != null
                            ? p.Contrat.Locataire.Prenom + " " +p.Contrat.Locataire.Nom
                            : TextesMessages.NonApplique
                })
                .ToListAsync();

        var groupes =
            paiements
                .GroupBy(p =>
                    p.ModePaiement)
                .Select(g => new
                {
                    ModePaiement =
                        g.Key,

                    TotalRecette =
                        g.Sum(x => x.Montant),

                    NombreTransactions =
                        g.Count(),

                    Details =
                        g.ToList()
                })
                .OrderByDescending(g =>
                    g.TotalRecette)
                .ToList();

        return Ok(new
        {
            DateDebut = dateDebut,
            DateFin = dateFin,

            Total =
                paiements.Sum(p =>
                    p.Montant),

            NombreTransactions =
                paiements.Count,

            ParMode = groupes
        });
    }

    // ============================================================
    // 6. HISTORIQUE LOCATAIRE
    // ============================================================

    [HttpGet("historique-locataire/{locataireId:guid}")]
    public async Task<IActionResult>
        GetHistoriquePaiementsLocataire(
            Guid locataireId)
    {
        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync();

        if (!societeId.HasValue)
        {
            return Forbid();
        }

        // On vérifie que le locataire possède au moins
        // un contrat appartenant à la société courante.
        var locataire =
            await _context.Utilisateurs
                .AsNoTracking()
                .Where(u =>
                    u.Id == locataireId &&
                    !u.EstSupprime)
                .Select(u => new
                {
                    u.Id,
                    u.Prenom,
                    u.Nom,
                    u.Email,
                    u.Telephone
                })
                .FirstOrDefaultAsync();

        if (locataire == null)
        {
            return NotFound(
                TextesMessages.LocataireIntrouvable);
        }

        var contratsLocataire =
            await _context.Contrats
                .AsNoTracking()
                .Where(c =>
                    !c.EstSupprime &&
                    c.LocataireId ==
                        locataireId &&
                    c.UniteLocative != null &&
                    c.UniteLocative.BienImmobilier != null &&
                    c.UniteLocative.BienImmobilier.SocieteId ==
                        societeId.Value)
                .Select(c => c.Id)
                .ToListAsync();

        if (contratsLocataire.Count == 0)
        {
            return Forbid();
        }

        var paiements =
            await _context.Paiements
                .AsNoTracking()
                .Where(p =>
                    p.Contrat != null &&
                    contratsLocataire.Contains(
                        p.Contrat.Id))
                .OrderByDescending(p =>
                    p.DatePaiement)
                .Select(p => new
                {
                    p.Id,
                    p.DatePaiement,
                    p.Montant,

                    Statut =
                        p.StatutTransaction.ToString(),

                    ModePaiement =
                        p.ModePaiement.ToString(),

                    NumeroQuittance =
                        p.NumeroQuittance,

                    UniteReference =
                        p.Contrat!
                            .UniteLocative!
                            .Reference,

                    BienNom =
                        p.Contrat!
                            .UniteLocative!
                            .BienImmobilier!
                            .Nom,

                    BienReference =
                        p.Contrat!
                            .UniteLocative!
                            .BienImmobilier!
                            .Reference
                })
                .ToListAsync();

        var totalVersements =
            paiements
                .Where(p =>
                    p.Statut ==
                    StatutTransaction.Confirme.ToString())
                .Sum(p => p.Montant);

        return Ok(new
        {
            Locataire = locataire,

            TotalVersements =
                totalVersements,

            NombrePaiements =
                paiements.Count,

            Historique =
                paiements
        });
    }

    // ============================================================
    // 7. RAPPORT DES CONTRATS
    // ============================================================

    [HttpGet("contrats")]
    public async Task<IActionResult> GetRapportContrats(
        [FromQuery] StatutContrat? statut)
    {
        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync();

        if (!societeId.HasValue)
        {
            return Forbid();
        }

        var maintenant =
            DateTime.UtcNow;

        var query =
            _context.Contrats
                .AsNoTracking()
                .Where(c =>
                    !c.EstSupprime &&
                    c.UniteLocative != null &&
                    c.UniteLocative.BienImmobilier != null &&
                    c.UniteLocative.BienImmobilier.SocieteId ==
                        societeId.Value);

        if (statut.HasValue)
        {
            query =
                query.Where(c =>
                    c.Statut ==
                    statut.Value);
        }

        var contrats =
            await query
                .OrderByDescending(c =>
                    c.DateDebut)
                .Select(c => new
                {
                    c.Id,

                    UniteReference =
                        c.UniteLocative!.Reference,

                    BienNom =
                        c.UniteLocative
                            .BienImmobilier!
                            .Nom,

                    BienReference =
                        c.UniteLocative
                            .BienImmobilier!
                            .Reference,

                    BienAdresse =
                        c.UniteLocative
                            .BienImmobilier!
                            .Adresse,

                    LocataireNom =
                        c.Locataire != null
                            ? $"{c.Locataire.Prenom} " +
                              $"{c.Locataire.Nom}"
                            : TextesMessages.NonApplique,

                    c.DateDebut,
                    c.DateFin,
                    c.MontantLoyer,
                    c.MontantCaution,

                    StatutContrat =
                        c.Statut.ToString(),

                    // Statut réel calculé à partir des dates
                    EstExpire =
                        c.DateFin < maintenant,

                    JoursAvantExpiration =
                        c.DateFin >= maintenant
                            ? (c.DateFin.Date -
                               maintenant.Date).Days
                            : 0,

                    c.DelaiJoursTolerance
                })
                .ToListAsync();

        return Ok(contrats);
    }

    // ============================================================
    // 8. RAPPORT DES IMPAYÉS
    // ============================================================

    [HttpGet("impayes")]
    public async Task<IActionResult> GetRapportImpayes()
    {
        try
        {
            var societeId =
                await GetSocieteIdUtilisateurConnecteAsync();

            if (!societeId.HasValue)
            {
                return Forbid();
            }

            var maintenant =
                DateTime.UtcNow;

            var premierJourMois =
                new DateTime(
                    maintenant.Year,
                    maintenant.Month,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc);

            var premierJourMoisSuivant =
                premierJourMois.AddMonths(1);

            var contratsActifs =
                await _context.Contrats
                    .AsNoTracking()
                    .Include(c => c.UniteLocative)
                        .ThenInclude(u =>
                            u.BienImmobilier)
                    .Include(c => c.Locataire)
                    .Include(c => c.Paiements)
                    .Where(c =>
                        c.Statut ==
                            StatutContrat.Actif &&
                        !c.EstSupprime &&
                        c.DateDebut <= maintenant &&
                        c.DateFin >= maintenant &&
                        c.UniteLocative != null &&
                        c.UniteLocative.BienImmobilier != null &&
                        c.UniteLocative.BienImmobilier.SocieteId ==
                            societeId.Value)
                    .ToListAsync();

            var impayes =
                await ConstruireImpayesAsync(
                    contratsActifs,
                    premierJourMois,
                    premierJourMoisSuivant,
                    maintenant);

            return Ok(new
            {
                NombreImpayes =
                    impayes.Count,

                MontantTotalImpayes =
                    impayes.Sum(i =>
                        i.Solde),

                Impayes =
                    impayes
            });
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new
                {
                    message =
                        "Erreur lors de la génération des impayés.",

                    detail =
                        ex.InnerException?.Message
                        ?? ex.Message
                });
        }
    }

    // ============================================================
    // 9. EXPORT PDF
    // ============================================================

    [HttpPost("export-pdf")]
    public IActionResult ExporterRapportPdf(
        [FromQuery] string typeRapport,
        [FromQuery] string titreRapport,
        [FromBody] JsonElement donnees)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(typeRapport))
            {
                return BadRequest(
                    "Le type de rapport est obligatoire.");
            }

            if (string.IsNullOrWhiteSpace(titreRapport))
            {
                return BadRequest(
                    "Le titre du rapport est obligatoire.");
            }

            var pdfBytes =
                _rapportPdfService.GenererRapportPdf(
                    typeRapport,
                    titreRapport,
                    donnees);

            return File(
                pdfBytes,
                "application/pdf",
                $"Rapport_{typeRapport}_" +
                $"{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new
                {
                    message =
                        "Erreur lors de la génération du PDF.",

                    detail =
                        ex.InnerException?.Message
                        ?? ex.Message
                });
        }
    }

    // ============================================================
    // MÉTHODES PRIVÉES
    // ============================================================

    /// <summary>
    /// Retourne la société de l'utilisateur actuellement connecté.
    /// Tous les rapports sont cloisonnés par cette société.
    /// </summary>
    private async Task<Guid?>
        GetSocieteIdUtilisateurConnecteAsync()
    {
        var userIdClaim =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
                userIdClaim,
                out var utilisateurId))
        {
            return null;
        }

        var utilisateur =
            await _context.Utilisateurs
                .AsNoTracking()
                .Where(u =>
                    u.Id == utilisateurId &&
                    !u.EstSupprime &&
                    u.Statut)
                .Select(u => new
                {
                    u.SocieteId
                })
                .FirstOrDefaultAsync();

        return utilisateur?.SocieteId;
    }

    /// <summary>
    /// Construit la liste des impayés du mois courant.
    /// Un paiement partiel laisse apparaître le solde restant.
    /// </summary>
    private Task<List<ImpayeRapportDto>>
        ConstruireImpayesAsync(
            IEnumerable<dynamic> contrats,
            DateTime debutMois,
            DateTime debutMoisSuivant,
            DateTime maintenant)
    {
        var result =
            new List<ImpayeRapportDto>();

        foreach (var contrat in contrats)
        {
            var dateLimite =
                debutMois.AddDays(
                    contrat.DelaiJoursTolerance);

            if (maintenant <= dateLimite)
            {
                continue;
            }

            decimal montantPaye = 0m;

            if (contrat.Paiements != null)
            {
                foreach (var paiement in contrat.Paiements)
                {
                    if (paiement.StatutTransaction ==
                            StatutTransaction.Confirme &&
                        paiement.DatePaiement >= debutMois &&
                        paiement.DatePaiement < debutMoisSuivant)
                    {
                        montantPaye +=
                            paiement.Montant;
                    }
                }
            }

            decimal montantLoyer =
                contrat.MontantLoyer;

            decimal solde =
                montantLoyer - montantPaye;

            if (solde <= 0)
            {
                continue;
            }

            var joursRetard =
                (maintenant.Date -
                 dateLimite.Date).Days;

            var uniteReference =
                contrat.UniteLocative?.Reference
                ?? TextesMessages.NonApplique;

            var bienNom =
                contrat.UniteLocative?
                    .BienImmobilier?
                    .Nom
                ?? TextesMessages.NonApplique;

            var bienReference =
                contrat.UniteLocative?
                    .BienImmobilier?
                    .Reference
                ?? TextesMessages.NonApplique;

            var locataireNom =
                contrat.Locataire == null
                    ? TextesMessages.NonApplique
                    : $"{contrat.Locataire.Prenom} " +
                      $"{contrat.Locataire.Nom}";

            var locataireTelephone =
                contrat.Locataire?.Telephone
                ?? TextesMessages.NonApplique;

            result.Add(
                new ImpayeRapportDto
                {
                    ContratId =
                        contrat.Id,

                    UniteReference =
                        uniteReference,

                    BienNom =
                        bienNom,

                    BienReference =
                        bienReference,

                    LocataireNom =
                        locataireNom,

                    LocataireTelephone =
                        locataireTelephone,

                    MontantLoyer =
                        montantLoyer,

                    MontantPaye =
                        montantPaye,

                    Solde =
                        solde,

                    DelaiToleranceJours =
                        contrat.DelaiJoursTolerance,

                    DateLimite =
                        dateLimite,

                    JoursDeRetard =
                        joursRetard
                });
        }

        return Task.FromResult(result);
    }

    // ============================================================
    // DTO INTERNE
    // ============================================================

    private sealed class ImpayeRapportDto
    {
        public Guid ContratId { get; set; }

        public string UniteReference { get; set; }
            = TextesMessages.NonApplique;

        public string BienNom { get; set; }
            = TextesMessages.NonApplique;

        public string BienReference { get; set; }
            = TextesMessages.NonApplique;

        public string LocataireNom { get; set; }
            = TextesMessages.NonApplique;

        public string LocataireTelephone { get; set; }
            = TextesMessages.NonApplique;

        public decimal MontantLoyer { get; set; }

        public decimal MontantPaye { get; set; }

        public decimal Solde { get; set; }

        public int DelaiToleranceJours { get; set; }

        public DateTime DateLimite { get; set; }

        public int JoursDeRetard { get; set; }
    }
}