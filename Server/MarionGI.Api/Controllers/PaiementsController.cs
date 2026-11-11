using System.IO.Compression;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MarionGI.Api.PaiementProvider;
using MarionGI.Domain.Entities;
using MarionGI.Domain.Enums;
using MarionGI.Infrastructure.Pdf;
using MarionGI.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MarionGI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaiementsController : ControllerBase
{
    private readonly MarionDbContext _context;
    private readonly IQuittancePdfService _pdfService;

    public PaiementsController(
        MarionDbContext context,
        IQuittancePdfService pdfService)
    {
        _context = context;
        _pdfService = pdfService;
    }

    // ============================================================
    // 1. INITIER UN PAIEMENT MOBILE MONEY
    // ============================================================

    [HttpPost("initier")]
    [Authorize(Roles = nameof(RoleUtilisateur.Locataire))]
    public async Task<IActionResult> InitierPaiement(
        [FromBody] InitierPaiementRequest request,
        [FromServices] IPaiementProvider paiementProvider,
        CancellationToken cancellationToken)
    {
        if (request == null ||
            request.ContratId == Guid.Empty ||
            request.Montant <= 0 ||
            string.IsNullOrWhiteSpace(request.Telephone))
        {
            return BadRequest(new
            {
                Message = "Le contrat, un montant positif et le téléphone sont obligatoires."
            });
        }

        if (!Enum.IsDefined(typeof(TypePaiement), request.TypePaiement))
        {
            return BadRequest(new
            {
                Message = "Le type de paiement est invalide."
            });
        }

        if (!Enum.IsDefined(typeof(ModePaiement), request.ModePaiement) ||
            request.ModePaiement == ModePaiement.Especes)
        {
            return BadRequest(new
            {
                Message = "Le mode de paiement Mobile Money est invalide."
            });
        }

        var utilisateurId = GetCurrentUserId();

        if (utilisateurId == null)
        {
            return Unauthorized(new
            {
                Message = "Impossible d'identifier le locataire connecté."
            });
        }

        var contrat = await _context.Contrats
            .AsNoTracking()
            .Include(c => c.Locataire)
            .Include(c => c.UniteLocative)
                .ThenInclude(u => u.BienImmobilier)
            .FirstOrDefaultAsync(c =>
                c.Id == request.ContratId &&
                c.LocataireId == utilisateurId.Value &&
                !c.EstSupprime &&
                !c.UniteLocative.EstSupprime &&
                !c.UniteLocative.BienImmobilier.EstSupprime,
                cancellationToken);

        if (contrat == null)
        {
            return NotFound(new
            {
                Message = "Contrat introuvable ou inaccessible."
            });
        }

        if (contrat.Statut != StatutContrat.Actif)
        {
            return BadRequest(new
            {
                Message = "Le contrat n'est pas actif."
            });
        }

        var erreurFinanciere = ValiderConditionsFinancieres(contrat,   request.TypePaiement);

        if (erreurFinanciere != null)
        {
            return BadRequest(erreurFinanciere);
        }

        if (!MoisEtTypeValides(
                contrat,
                request.TypePaiement,
                request.MoisLoyer))
        {
            return BadRequest(new
            {
                Message = "Un mois valide est obligatoire pour un loyer ou des charges locatives. Il ne doit pas être renseigné pour une caution ou un autre paiement."
            });
        }

        var moisLoyer = request.MoisLoyer.HasValue
            ? NormaliserMoisLoyer(request.MoisLoyer.Value)
            : (DateTime?)null;

        var montantDu = CalculerMontantDu(
            contrat,
            request.TypePaiement,
            request.Montant);

        var montantDejaConfirme =
            await CalculerMontantDejaConfirmeAsync(
                contrat.Id,
                request.TypePaiement,
                moisLoyer,
                cancellationToken);

        var montantRestant = request.TypePaiement == TypePaiement.Autre
            ? (decimal?)null
            : montantDu - montantDejaConfirme;

        if (montantRestant.HasValue && montantRestant.Value <= 0)
        {
            return Conflict(new
            {
                Message = "Le montant exigible pour ce type de paiement est déjà intégralement réglé.",
                TypePaiement = request.TypePaiement,
                MontantDu = montantDu,
                MontantDejaConfirme = montantDejaConfirme
            });
        }

        if (montantRestant.HasValue &&
            request.Montant > montantRestant.Value)
        {
            return BadRequest(new
            {
                Message = "Le versement dépasse le solde exigible.",
                TypePaiement = request.TypePaiement,
                MontantDu = montantDu,
                MontantDejaConfirme = montantDejaConfirme,
                MontantRestant = montantRestant.Value
            });
        }

        var paiementEnAttente = await ExistePaiementEnAttenteAsync(
            contrat.Id,
            request.TypePaiement,
            moisLoyer,
            cancellationToken);

        if (paiementEnAttente)
        {
            return Conflict(new
            {
                Message = "Un paiement de ce type est déjà en cours pour ce contrat ou cette échéance."
            });
        }

        var maintenant = DateTime.UtcNow;

        var paiement = new Paiement
        {
            Id = Guid.NewGuid(),
            ContratId = contrat.Id,
            TypePaiement = request.TypePaiement,
            MoisLoyer = moisLoyer,
            MontantDu = montantDu,
            Montant = request.Montant,

            MontantLoyerEncaisse =
                request.TypePaiement == TypePaiement.Loyer
                    ? request.Montant
                    : 0m,

            MontantChargesLocativesEncaisse =
                request.TypePaiement == TypePaiement.ChargesLocatives
                    ? request.Montant
                    : 0m,

            DatePaiement = maintenant,
            ModePaiement = request.ModePaiement,
            StatutTransaction = StatutTransaction.EnAttente,
            NumeroQuittance = GenererNumeroQuittance(),
            EnregistreParUtilisateurId = utilisateurId.Value,
            DateCreation = maintenant,
            EstSupprime = false
        };

        _context.Paiements.Add(paiement);

        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            var description = ConstruireDescriptionPaiement(
                request.TypePaiement,
                moisLoyer,
                paiement.NumeroQuittance);

            var resultat = await paiementProvider.InitierAsync(
                new InitierPaiementContext(
                    PaiementId: paiement.Id,
                    Montant: paiement.Montant,
                    Telephone: request.Telephone.Trim(),
                    EmailClient: contrat.Locataire?.Email
                        ?? "client@mariongi.cm",
                    ModePaiement: paiement.ModePaiement,
                    Description: description
                ));

            if (!resultat.Succes)
            {
                paiement.StatutTransaction = StatutTransaction.Echoue;
                paiement.MotifEchec = resultat.MessageErreur;

                await _context.SaveChangesAsync(cancellationToken);

                return BadRequest(new
                {
                    Message = resultat.MessageErreur
                        ?? "Impossible d'initialiser le paiement."
                });
            }

            paiement.ReferenceTransactionOperateur =
                resultat.ReferenceExterne;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                Message = "Demande de paiement envoyée sur le mobile.",
                paiement.Id,
                paiement.TypePaiement,
                paiement.NumeroQuittance,
                paiement.MoisLoyer,
                paiement.Montant,
                paiement.MontantLoyerEncaisse,
                paiement.MontantChargesLocativesEncaisse,
                paiement.MontantDu,
                MontantDejaConfirme = montantDejaConfirme,
                MontantRestant = montantRestant.HasValue
                    ? montantRestant.Value - paiement.Montant
                    : (decimal?)null,
                paiement.ReferenceTransactionOperateur,
                Statut = paiement.StatutTransaction.ToString()
            });
        }
        catch
        {
            paiement.StatutTransaction = StatutTransaction.Echoue;
            paiement.MotifEchec =
                "Erreur lors de l'initialisation auprès du fournisseur.";

            await _context.SaveChangesAsync(cancellationToken);

            throw;
        }
    }

    // ============================================================
    // 2. ENREGISTRER UN PAIEMENT EN ESPÈCES
    // ============================================================

    [HttpPost("especes")]
    [Authorize(Roles =
        nameof(RoleUtilisateur.Administrateur) + "," +
        nameof(RoleUtilisateur.Gestionnaire))]
    public async Task<IActionResult> EnregistrerPaiementEspeces(
        [FromBody] PaiementEspecesRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null ||
            request.ContratId == Guid.Empty ||
            request.Montant <= 0)
        {
            return BadRequest(new
            {
                Message = "Un contrat valide et un montant positif sont obligatoires."
            });
        }

        if (!Enum.IsDefined(typeof(TypePaiement), request.TypePaiement))
        {
            return BadRequest(new
            {
                Message = "Le type de paiement est invalide."
            });
        }

        var utilisateurId = GetCurrentUserId();

        var societeId = await GetSocieteIdUtilisateurConnecteAsync(
            cancellationToken);

        if (utilisateurId == null || societeId == null)
        {
            return Unauthorized(new
            {
                Message = "Impossible d'identifier l'utilisateur ou son propriétaire."
            });
        }

        var contrat = await _context.Contrats
            .Include(c => c.UniteLocative)
                .ThenInclude(u => u.BienImmobilier)
            .FirstOrDefaultAsync(c =>
                c.Id == request.ContratId &&
                !c.EstSupprime &&
                !c.UniteLocative.EstSupprime &&
                !c.UniteLocative.BienImmobilier.EstSupprime &&
                c.UniteLocative.BienImmobilier.SocieteId == societeId.Value,
                cancellationToken);

        if (contrat == null)
        {
            return NotFound(new
            {
                Message = "Contrat introuvable ou inaccessible."
            });
        }

        if (contrat.Statut != StatutContrat.Actif)
        {
            return BadRequest(new
            {
                Message = "Le contrat n'est pas actif."
            });
        }
        var erreurFinanciere = ValiderConditionsFinancieres(contrat,   request.TypePaiement);

        if (erreurFinanciere != null)
        {
            return BadRequest(erreurFinanciere);
        }

        if (!MoisEtTypeValides(
                contrat,
                request.TypePaiement,
                request.MoisLoyer))
        {
            return BadRequest(new
            {
                Message = "Un mois valide est obligatoire pour un loyer ou des charges locatives. Il ne doit pas être renseigné pour une caution ou un autre paiement."
            });
        }

        var moisLoyer = request.MoisLoyer.HasValue
            ? NormaliserMoisLoyer(request.MoisLoyer.Value)
            : (DateTime?)null;

        var montantDu = CalculerMontantDu(
            contrat,
            request.TypePaiement,
            request.Montant);

        var montantDejaConfirme =
            await CalculerMontantDejaConfirmeAsync(
                contrat.Id,
                request.TypePaiement,
                moisLoyer,
                cancellationToken);

        var montantRestant = request.TypePaiement == TypePaiement.Autre
            ? (decimal?)null
            : montantDu - montantDejaConfirme;

        if (montantRestant.HasValue &&
            (montantRestant.Value <= 0 ||
             request.Montant > montantRestant.Value))
        {
            return Conflict(new
            {
                Message = montantRestant.Value <= 0
                    ? "Le montant exigible pour ce type de paiement est déjà intégralement réglé."
                    : "Le versement dépasse le solde exigible.",
                TypePaiement = request.TypePaiement,
                MontantDu = montantDu,
                MontantDejaConfirme = montantDejaConfirme,
                MontantRestant = Math.Max(0m, montantRestant.Value)
            });
        }

        var paiementEnAttente = await ExistePaiementEnAttenteAsync(
            contrat.Id,
            request.TypePaiement,
            moisLoyer,
            cancellationToken);

        if (paiementEnAttente)
        {
            return Conflict(new
            {
                Message = "Une transaction Mobile Money de ce type est déjà en cours pour ce contrat ou cette échéance."
            });
        }

        var maintenant = DateTime.UtcNow;

        var paiement = new Paiement
        {
            Id = Guid.NewGuid(),
            ContratId = contrat.Id,
            TypePaiement = request.TypePaiement,
            MoisLoyer = moisLoyer,
            MontantDu = montantDu,
            Montant = request.Montant,

            MontantLoyerEncaisse =
                request.TypePaiement == TypePaiement.Loyer
                    ? request.Montant
                    : 0m,

            MontantChargesLocativesEncaisse =
                request.TypePaiement == TypePaiement.ChargesLocatives
                    ? request.Montant
                    : 0m,

            DatePaiement = maintenant,
            DateConfirmation = maintenant,
            ModePaiement = ModePaiement.Especes,
            StatutTransaction = StatutTransaction.Confirme,
            NumeroQuittance = GenererNumeroQuittance(),
            EnregistreParUtilisateurId = utilisateurId.Value,
            DateCreation = maintenant,
            EstSupprime = false
        };

        _context.Paiements.Add(paiement);

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            Message = "Paiement en espèces enregistré.",
            paiement.Id,
            paiement.TypePaiement,
            paiement.NumeroQuittance,
            paiement.MoisLoyer,
            paiement.Montant,
            paiement.MontantLoyerEncaisse,
            paiement.MontantChargesLocativesEncaisse,
            paiement.MontantDu,
            MontantDejaConfirme = montantDejaConfirme + paiement.Montant,
            MontantRestant = montantRestant.HasValue
                ? Math.Max(0m, montantRestant.Value - paiement.Montant)
                : (decimal?)null,
            paiement.DatePaiement,
            paiement.DateConfirmation,
            Statut = paiement.StatutTransaction.ToString()
        });
    }

    // ============================================================
    // 3. CONSULTER LE STATUT D'UN PAIEMENT
    // ============================================================

    [HttpGet("{id:guid}/statut")]
    [Authorize(Roles =
        nameof(RoleUtilisateur.Administrateur) + "," +
        nameof(RoleUtilisateur.Gestionnaire) + "," +
        nameof(RoleUtilisateur.Locataire))]
    public async Task<IActionResult> GetStatut(
        Guid id,
        CancellationToken cancellationToken)
    {
        var paiement = await GetPaiementAccessibleAsync(
            id, cancellationToken);

        if (paiement == null)
        {
            return NotFound(new
            {
                Message = "Paiement introuvable ou inaccessible."
            });
        }

        var montantConfirme =
            await CalculerMontantDejaConfirmeAsync(
                paiement.ContratId,
                paiement.TypePaiement,
                paiement.MoisLoyer,
                cancellationToken);

        decimal? montantRestant = paiement.TypePaiement
            == TypePaiement.Autre
                ? null
                : Math.Max(0m, paiement.MontantDu - montantConfirme);

        return Ok(new
        {
            paiement.Id,
            paiement.ContratId,
            paiement.TypePaiement,
            paiement.NumeroQuittance,
            paiement.MoisLoyer,
            paiement.Montant,
            paiement.MontantLoyerEncaisse,
            paiement.MontantChargesLocativesEncaisse,
            paiement.MontantDu,
            MontantTotalConfirme = montantConfirme,
            MontantRestant = montantRestant,
            paiement.ModePaiement,
            paiement.StatutTransaction,
            paiement.DatePaiement,
            paiement.DateConfirmation,
            paiement.ReferenceTransactionOperateur,
            paiement.FraisTransaction,
            paiement.MotifEchec,
            paiement.Observations
        });
    }

    // ============================================================
    // 4. TÉLÉCHARGER UNE QUITTANCE
    // ============================================================

    [HttpGet("{id:guid}/quittance")]
    [Authorize(Roles =
        nameof(RoleUtilisateur.Administrateur) + "," +
        nameof(RoleUtilisateur.Gestionnaire) + "," +
        nameof(RoleUtilisateur.Locataire))]
    public async Task<IActionResult> TelechargerQuittance(
        Guid id,
        CancellationToken cancellationToken)
    {
        var paiement = await GetPaiementAccessibleAsync(
            id, cancellationToken, inclureRelations: true);

        if (paiement == null)
        {
            return NotFound(new
            {
                Message = "Paiement introuvable ou inaccessible."
            });
        }

        if (paiement.StatutTransaction != StatutTransaction.Confirme)
        {
            return BadRequest(new
            {
                Message = "Une quittance ne peut être générée que pour un paiement confirmé."
            });
        }

        var pdf = _pdfService.GenererQuittance(paiement);

        return File(
            pdf,
            "application/pdf",
            $"Quittance_{paiement.NumeroQuittance}.pdf");
    }

    // ============================================================
    // 5. LISTE DES PAIEMENTS
    // ============================================================

    [HttpGet]
    [Authorize(Roles =
        nameof(RoleUtilisateur.Administrateur) + "," +
        nameof(RoleUtilisateur.Gestionnaire) + "," +
        nameof(RoleUtilisateur.Locataire))]
    public async Task<IActionResult> GetPaiements(
        CancellationToken cancellationToken)
    {
        var query = _context.Paiements
            .AsNoTracking()
            .Where(p =>
                !p.EstSupprime &&
                !p.Contrat.EstSupprime &&
                !p.Contrat.UniteLocative.EstSupprime &&
                !p.Contrat.UniteLocative.BienImmobilier.EstSupprime);

        if (User.IsInRole(nameof(RoleUtilisateur.Locataire)))
        {
            var utilisateurId = GetCurrentUserId();

            if (utilisateurId == null)
            {
                return Unauthorized();
            }

            query = query.Where(p =>
                p.Contrat.LocataireId == utilisateurId.Value);
        }
        else
        {
            var societeId = await GetSocieteIdUtilisateurConnecteAsync(
                cancellationToken);

            if (societeId == null)
            {
                return Unauthorized();
            }

            query = query.Where(p =>
                p.Contrat.UniteLocative.BienImmobilier.SocieteId ==
                societeId.Value);
        }

        var paiements = await query
            .OrderByDescending(p => p.MoisLoyer)
            .ThenByDescending(p => p.DatePaiement)
            .Select(p => new PaiementListItemDto
            {
                Id = p.Id,
                ContratId = p.ContratId,
                DatePaiement = p.DatePaiement,
                DateConfirmation = p.DateConfirmation,
                TypePaiement = p.TypePaiement,
                MoisLoyer = p.MoisLoyer,
                Montant = p.Montant,
                MontantLoyerEncaisse = p.MontantLoyerEncaisse,
                MontantChargesLocativesEncaisse =
                    p.MontantChargesLocativesEncaisse,
                MontantDu = p.MontantDu,
                Mode = p.ModePaiement,
                Statut = p.StatutTransaction,
                NumeroQuittance = p.NumeroQuittance,
                ReferenceTransactionOperateur = p.ReferenceTransactionOperateur,
                FraisTransaction = p.FraisTransaction,
                MotifEchec = p.MotifEchec,
                BienNom = p.Contrat.UniteLocative.BienImmobilier.Nom,
                BienReference = p.Contrat.UniteLocative.BienImmobilier.Reference,
                UniteReference = p.Contrat.UniteLocative.Reference,
                LocataireNom = (
                    p.Contrat.Locataire.Nom + " " +
                    p.Contrat.Locataire.Prenom).Trim()
            })
            .ToListAsync(cancellationToken);

        return Ok(paiements);
    }

    // ============================================================
    // 6. TÉLÉCHARGER TOUTES LES QUITTANCES D'UN CONTRAT
    // ============================================================

    [HttpGet("contrat/{contratId:guid}/toutes-les-quittances")]
    [Authorize(Roles =
        nameof(RoleUtilisateur.Administrateur) + "," +
        nameof(RoleUtilisateur.Gestionnaire) + "," +
        nameof(RoleUtilisateur.Locataire))]
    public async Task<IActionResult> TelechargerToutesLesQuittances(
        Guid contratId,
        CancellationToken cancellationToken)
    {
        if (!await EstContratAccessibleAsync(
                contratId,
                cancellationToken))
        {
            return NotFound(new
            {
                Message = "Contrat introuvable ou inaccessible."
            });
        }

        var paiements = await _context.Paiements
            .Include(p => p.Contrat)
                .ThenInclude(c => c.Locataire)
            .Include(p => p.Contrat)
                .ThenInclude(c => c.UniteLocative)
                    .ThenInclude(u => u.BienImmobilier)
            .Where(p =>
                p.ContratId == contratId &&
                p.StatutTransaction == StatutTransaction.Confirme &&
                !p.EstSupprime &&
                !p.Contrat.EstSupprime &&
                !p.Contrat.UniteLocative.EstSupprime &&
                !p.Contrat.UniteLocative.BienImmobilier.EstSupprime)
            .OrderBy(p => p.MoisLoyer)
            .ThenBy(p => p.DatePaiement)
            .ToListAsync(cancellationToken);

        if (paiements.Count == 0)
        {
            return NotFound(new
            {
                Message = "Aucun paiement confirmé trouvé pour ce contrat."
            });
        }

        await using var memoryStream = new MemoryStream();

        using (var archive = new ZipArchive(
            memoryStream,
            ZipArchiveMode.Create,
            leaveOpen: true))
        {
            foreach (var paiement in paiements)
            {
                var pdf = _pdfService.GenererQuittance(paiement);

                var entry = archive.CreateEntry(
                    $"Quittance_{paiement.NumeroQuittance}.pdf");

                await using var entryStream = entry.Open();

                await entryStream.WriteAsync(
                    pdf.AsMemory(),
                    cancellationToken);
            }
        }

        return File(
            memoryStream.ToArray(),
            "application/zip",
            $"Quittances_Contrat_{contratId}.zip");
    }

    // ============================================================
    // 7. WEBHOOK NOTCHPAY
    // ============================================================

    [HttpPost("webhook/notchpay")]
    [AllowAnonymous]
    public async Task<IActionResult> WebhookNotchpay(
        [FromServices] IOptions<NotchpayPaiementOptions> options,
        CancellationToken cancellationToken)
    {
        Request.EnableBuffering();

        using var reader = new StreamReader(
            Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return BadRequest(new { error = "Payload vide." });
        }

        var signature = Request.Headers["x-notch-signature"].ToString();
        var hashKey = options.Value.WebhookHashKey;

        if (string.IsNullOrWhiteSpace(hashKey))
        {
            return StatusCode(500, new
            {
                error = "La clé de signature du webhook n'est pas configurée."
            });
        }

        if (!VerifierSignature(rawBody, signature, hashKey))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "Signature invalide."
            });
        }

        NotchpayWebhookPayload? payload;

        try
        {
            payload = JsonSerializer.Deserialize<NotchpayWebhookPayload>(rawBody);
        }
        catch (JsonException)
        {
            return BadRequest(new { error = "Payload webhook invalide." });
        }

        if (payload?.Data?.Reference == null)
        {
            return BadRequest(new { error = "Référence de paiement absente." });
        }

        var paiement = await _context.Paiements
            .FirstOrDefaultAsync(p =>
                p.ReferenceTransactionOperateur == payload.Data.Reference &&
                !p.EstSupprime,
                cancellationToken);

        if (paiement == null)
        {
            return NotFound(new { error = "Paiement introuvable." });
        }

        if (paiement.StatutTransaction != StatutTransaction.EnAttente)
        {
            return Ok(new { message = "Événement déjà traité." });
        }

        switch (payload.Event)
        {
            case "payment.complete":
                {
                    // Les anciens paiements sans TypePaiement conservent
                    // leur ancien fonctionnement : échéance mensuelle globale.
                    if (paiement.TypePaiement == null)
                    {
                        var dejaConfirme = await _context.Paiements
                            .Where(p =>
                                p.ContratId == paiement.ContratId &&
                                p.TypePaiement == null &&
                                p.MoisLoyer == paiement.MoisLoyer &&
                                p.Id != paiement.Id &&
                                !p.EstSupprime &&
                                p.StatutTransaction == StatutTransaction.Confirme)
                            .SumAsync(
                                p => (decimal?)p.Montant,
                                cancellationToken) ?? 0m;

                        if (dejaConfirme + paiement.Montant >
                            paiement.MontantDu)
                        {
                            paiement.StatutTransaction =
                                StatutTransaction.Echoue;

                            paiement.MotifEchec =
                                "La confirmation dépasserait le montant exigible de l'ancienne échéance.";

                            await _context.SaveChangesAsync(cancellationToken);

                            return Conflict(new
                            {
                                error = "Le montant confirmé dépasse le solde exigible."
                            });
                        }
                    }
                    else if (paiement.TypePaiement != TypePaiement.Autre)
                    {
                        var dejaConfirme =
                            await CalculerMontantDejaConfirmeAsync(
                                paiement.ContratId,
                                paiement.TypePaiement.Value,
                                paiement.MoisLoyer,
                                cancellationToken,
                                paiement.Id);

                        if (dejaConfirme + paiement.Montant >
                            paiement.MontantDu)
                        {
                            paiement.StatutTransaction =
                                StatutTransaction.Echoue;

                            paiement.MotifEchec =
                                "La confirmation dépasserait le montant exigible pour ce type de paiement.";

                            await _context.SaveChangesAsync(cancellationToken);

                            return Conflict(new
                            {
                                error = "Le montant confirmé dépasse le solde exigible."
                            });
                        }
                    }

                    // La ventilation a été figée à l'initiation.
                    // Elle n'est jamais recalculée à partir du contrat actuel.
                    paiement.StatutTransaction = StatutTransaction.Confirme;
                    paiement.DateConfirmation = DateTime.UtcNow;
                    paiement.DatePaiement = DateTime.UtcNow;
                    paiement.MotifEchec = null;

                    break;
                }

            case "payment.failed":
                paiement.StatutTransaction = StatutTransaction.Echoue;
                paiement.MotifEchec =
                    "Le fournisseur a signalé l'échec de la transaction.";
                break;

            default:
                return Ok(new
                {
                    message = "Événement reçu mais non pris en charge."
                });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Webhook traité avec succès." });
    }

    // ============================================================
    // 8. VÉRIFIER L'ACCÈS À UN CONTRAT
    // ============================================================

    private async Task<bool> EstContratAccessibleAsync(
        Guid contratId,
        CancellationToken cancellationToken)
    {
        var query = _context.Contrats
            .AsNoTracking()
            .Where(c =>
                c.Id == contratId &&
                !c.EstSupprime &&
                !c.UniteLocative.EstSupprime &&
                !c.UniteLocative.BienImmobilier.EstSupprime);

        if (User.IsInRole(nameof(RoleUtilisateur.Locataire)))
        {
            var utilisateurId = GetCurrentUserId();

            if (utilisateurId == null)
            {
                return false;
            }

            query = query.Where(c =>
                c.LocataireId == utilisateurId.Value);
        }
        else
        {
            var societeId = await GetSocieteIdUtilisateurConnecteAsync(
                cancellationToken);

            if (societeId == null)
            {
                return false;
            }

            query = query.Where(c =>
                c.UniteLocative.BienImmobilier.SocieteId == societeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    // ============================================================
    // 9. RÉCUPÉRER UN PAIEMENT ACCESSIBLE
    // ============================================================

    private async Task<Paiement?> GetPaiementAccessibleAsync(
        Guid paiementId,
        CancellationToken cancellationToken,
        bool inclureRelations = false)
    {
        if (paiementId == Guid.Empty)
        {
            return null;
        }

        IQueryable<Paiement> query = _context.Paiements;

        if (inclureRelations)
        {
            query = query
                .Include(p => p.Contrat)
                    .ThenInclude(c => c.Locataire)
                .Include(p => p.Contrat)
                    .ThenInclude(c => c.UniteLocative)
                        .ThenInclude(u => u.BienImmobilier);
        }

        query = query.Where(p =>
            p.Id == paiementId &&
            !p.EstSupprime &&
            !p.Contrat.EstSupprime &&
            !p.Contrat.UniteLocative.EstSupprime &&
            !p.Contrat.UniteLocative.BienImmobilier.EstSupprime);

        if (User.IsInRole(nameof(RoleUtilisateur.Locataire)))
        {
            var utilisateurId = GetCurrentUserId();

            if (utilisateurId == null)
            {
                return null;
            }

            query = query.Where(p =>
                p.Contrat.LocataireId == utilisateurId.Value);
        }
        else
        {
            var societeId = await GetSocieteIdUtilisateurConnecteAsync(
                cancellationToken);

            if (societeId == null)
            {
                return null;
            }

            query = query.Where(p =>
                p.Contrat.UniteLocative.BienImmobilier.SocieteId ==
                societeId.Value);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    // ============================================================
    // 10. CALCULER LE MONTANT EXIGIBLE SELON LE TYPE
    // ============================================================

    private static decimal CalculerMontantDu(
        Contrat contrat,
        TypePaiement typePaiement,
        decimal montantAutre)
    {
        return typePaiement switch
        {
            TypePaiement.Loyer =>
                contrat.MontantLoyer,

            TypePaiement.ChargesLocatives =>
                contrat.MontantChargesLocatives ?? 0m,

            TypePaiement.Caution =>
                contrat.MontantCaution,

            // Aucun plafond contractuel prédéfini pour Autre.
            // MontantDu représente ici le montant de la transaction.
            TypePaiement.Autre =>
                montantAutre,

            _ => 0m
        };
    }

    // ============================================================
    // 11. VÉRIFIER LES CONDITIONS FINANCIÈRES
    // ============================================================

    private static string? ValiderConditionsFinancieres(
     Contrat contrat,
     TypePaiement typePaiement)
    {
        return typePaiement switch
        {
            TypePaiement.Loyer when contrat.MontantLoyer <= 0m =>
                "Le montant du loyer n'est pas correctement configuré dans ce contrat.",

            TypePaiement.ChargesLocatives
                when contrat.MontantChargesLocatives.GetValueOrDefault() <= 0m =>
                "Aucune charge locative mensuelle n'est prévue dans ce contrat.",

            TypePaiement.Caution when contrat.MontantCaution <= 0m =>
                "Le montant de la caution n'est pas configuré dans ce contrat.",

            TypePaiement.Autre => null,

            TypePaiement.Loyer or
            TypePaiement.ChargesLocatives or
            TypePaiement.Caution => null,

            _ => "Le type de paiement sélectionné est invalide."
        };
    }

    // ============================================================
    // 12. VÉRIFIER LA COHÉRENCE DU MOIS AVEC LE TYPE
    // ============================================================
    private static bool MoisEtTypeValides(
        Contrat contrat,
        TypePaiement typePaiement,
        DateTime? moisLoyer)
    {
        if (typePaiement == TypePaiement.Loyer ||
            typePaiement == TypePaiement.ChargesLocatives)
        {
            if (!moisLoyer.HasValue)
                return false;

            var moisNormalise = NormaliserMoisLoyer(moisLoyer.Value);

            return MoisDansPeriodeContrat(contrat, moisNormalise);
        }

        return moisLoyer == null;
    }

    // ============================================================
    // 13. CALCULER LE TOTAL DÉJÀ CONFIRMÉ
    // ============================================================

    private async Task<decimal> CalculerMontantDejaConfirmeAsync(
     Guid contratId,
     TypePaiement? typePaiement,
     DateTime? moisLoyer,
     CancellationToken cancellationToken,
     Guid? paiementExcluId = null)
    {
        var query = _context.Paiements
            .AsNoTracking()
            .Where(p =>
                p.ContratId == contratId &&
                !p.EstSupprime &&
                p.StatutTransaction == StatutTransaction.Confirme);

        if (paiementExcluId.HasValue)
        {
            query = query.Where(
                p => p.Id != paiementExcluId.Value);
        }

        if (typePaiement == null)
        {
            query = query.Where(p =>
                p.TypePaiement == null &&
                p.MoisLoyer == moisLoyer);
        }
        else if (typePaiement == TypePaiement.Loyer)
        {
            if (!moisLoyer.HasValue)
                return 0m;

            var moisNormalise = NormaliserMoisLoyer(
                moisLoyer.Value);

            query = query.Where(p =>
                p.TypePaiement == TypePaiement.Loyer &&
                p.MoisLoyer == moisNormalise);
        }
        else
        {
            // Charges locatives et caution :
            // cumul sur l'ensemble du contrat.
            query = query.Where(p =>
                p.TypePaiement == typePaiement);
        }

        return await query.SumAsync(
            p => (decimal?)p.Montant,
            cancellationToken) ?? 0m;
    }
    // ============================================================
    // 14. VÉRIFIER L'EXISTENCE D'UN PAIEMENT EN ATTENTE
    // ============================================================

    private async Task<bool> ExistePaiementEnAttenteAsync(
     Guid contratId,
     TypePaiement typePaiement,
     DateTime? moisLoyer,
     CancellationToken cancellationToken)
    {
        var query = _context.Paiements
            .Where(p =>
                p.ContratId == contratId &&
                p.TypePaiement == typePaiement &&
                !p.EstSupprime &&
                p.StatutTransaction == StatutTransaction.EnAttente);

        if (typePaiement == TypePaiement.Loyer)
        {
            if (!moisLoyer.HasValue)
                return false;

            var moisNormalise = NormaliserMoisLoyer(
                moisLoyer.Value);

            query = query.Where(
                p => p.MoisLoyer == moisNormalise);
        }

        // Charges, caution et autres :
        // une transaction du même type en attente bloque
        // une nouvelle transaction de ce type sur le contrat.

        return await query.AnyAsync(cancellationToken);
    }
    // ============================================================
    // 15. CONSTRUIRE LA DESCRIPTION DU PAIEMENT
    // ============================================================

    private static string ConstruireDescriptionPaiement(
        TypePaiement typePaiement,
        DateTime? moisLoyer,
        string numeroQuittance)
    {
        var libelle = typePaiement switch
        {
            TypePaiement.Loyer => "Loyer",
            TypePaiement.ChargesLocatives => "Charges locatives",
            TypePaiement.Caution => "Caution",
            TypePaiement.Autre => "Autre paiement",
            _ => "Paiement"
        };

        var periode = moisLoyer.HasValue
            ? $" {moisLoyer.Value:MM/yyyy}"
            : string.Empty;

        return $"{libelle}{periode} - {numeroQuittance}";
    }

    // ============================================================
    // 16. SOCIÉTÉ DE L'UTILISATEUR CONNECTÉ
    // ============================================================

    private async Task<Guid?> GetSocieteIdUtilisateurConnecteAsync(
        CancellationToken cancellationToken)
    {
        var utilisateurId = GetCurrentUserId();

        if (utilisateurId == null)
        {
            return null;
        }

        return await _context.Utilisateurs
            .AsNoTracking()
            .Where(u =>
                u.Id == utilisateurId.Value &&
                !u.EstSupprime &&
                u.Statut)
            .Select(u => (Guid?)u.SocieteId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    // ============================================================
    // 17. UTILISATEUR CONNECTÉ
    // ============================================================

    private Guid? GetCurrentUserId()
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("Id");

        return Guid.TryParse(userId, out var parsedUserId)
            ? parsedUserId
            : null;
    }

    // ============================================================
    // 18. NORMALISER LE MOIS DE LOYER
    // ============================================================

    private static DateTime NormaliserMoisLoyer(DateTime moisLoyer)
    {
        return new DateTime(
            moisLoyer.Year,
            moisLoyer.Month,
            1);
    }

    private static bool MoisDansPeriodeContrat(
        Contrat contrat,
        DateTime moisLoyer)
    {
        var debutMois = NormaliserMoisLoyer(moisLoyer);
        var finMois = debutMois.AddMonths(1).AddTicks(-1);

        return debutMois <= contrat.DateFin &&
               finMois >= contrat.DateDebut;
    }

    // ============================================================
    // 19. SIGNATURE WEBHOOK
    // ============================================================

    private static bool VerifierSignature(
        string payload,
        string signature,
        string hashKey)
    {
        if (string.IsNullOrWhiteSpace(signature) ||
            string.IsNullOrWhiteSpace(hashKey))
        {
            return false;
        }

        using var hmac = new HMACSHA256(
            Encoding.UTF8.GetBytes(hashKey));

        var computedHex = Convert.ToHexString(
            hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)))
            .ToLowerInvariant();

        var expectedBytes = Encoding.UTF8.GetBytes(computedHex);

        var providedBytes = Encoding.UTF8.GetBytes(
            signature.Trim().ToLowerInvariant());

        return expectedBytes.Length == providedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(
                   expectedBytes,
                   providedBytes);
    }

    // ============================================================
    // 20. NUMÉRO DE QUITTANCE
    // ============================================================

    private static string GenererNumeroQuittance()
    {
        return $"Q-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }
}

// =================================================================
// DTO : LISTE DES PAIEMENTS
// =================================================================

public sealed class PaiementListItemDto
{
    public Guid Id { get; set; }
    public Guid ContratId { get; set; }

    public DateTime DatePaiement { get; set; }
    public DateTime? DateConfirmation { get; set; }

    public TypePaiement? TypePaiement { get; set; }
    public DateTime? MoisLoyer { get; set; }

    public decimal Montant { get; set; }
    public decimal? MontantLoyerEncaisse { get; set; }
    public decimal? MontantChargesLocativesEncaisse { get; set; }
    public decimal MontantDu { get; set; }

    public ModePaiement Mode { get; set; }
    public StatutTransaction Statut { get; set; }

    public string NumeroQuittance { get; set; } = string.Empty;
    public string? ReferenceTransactionOperateur { get; set; }
    public decimal? FraisTransaction { get; set; }
    public string? MotifEchec { get; set; }

    public string LocataireNom { get; set; } = "N/A";
    public string UniteReference { get; set; } = "N/A";
    public string BienReference { get; set; } = "N/A";
    public string BienNom { get; set; } = "N/A";
}

// =================================================================
// DTO : PAIEMENT MOBILE MONEY
// =================================================================

public record InitierPaiementRequest(
    Guid ContratId,
    decimal Montant,
    TypePaiement TypePaiement,
    ModePaiement ModePaiement,
    string Telephone,
    DateTime? MoisLoyer);

// =================================================================
// DTO : PAIEMENT ESPÈCES
// =================================================================

public record PaiementEspecesRequest(
    Guid ContratId,
    decimal Montant,
    TypePaiement TypePaiement,
    DateTime? MoisLoyer);
