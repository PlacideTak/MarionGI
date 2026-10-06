using MarionGI.Api.PaiementProvider;
using MarionGI.Domain.Entities;
using MarionGI.Domain.Enums;
using MarionGI.Infrastructure.Pdf;
using MarionGI.Persistence.Context;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using System.IO.Compression;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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
    //
    // UNIQUEMENT LOCATAIRE
    //
    // Le locataire peut uniquement payer un de SES contrats
    // et uniquement si celui-ci est actif.
    // ============================================================

    [HttpPost("initier")]
    [Authorize(Roles = nameof(RoleUtilisateur.Locataire))]
    public async Task<IActionResult> InitierPaiement(
        [FromBody] InitierPaiementRequest request,
        [FromServices] IPaiementProvider paiementProvider)
    {
        // --------------------------------------------------------
        // Validation
        // --------------------------------------------------------

        if (request.ContratId == Guid.Empty)
        {
            return BadRequest(
                "Le contrat est obligatoire.");
        }

        if (request.Montant <= 0)
        {
            return BadRequest(
                "Le montant du paiement doit être supérieur à zéro.");
        }

        if (string.IsNullOrWhiteSpace(request.Telephone))
        {
            return BadRequest(
                "Le numéro de téléphone est obligatoire.");
        }

        if (request.ModePaiement == ModePaiement.Especes)
        {
            return BadRequest(
                "Utilisez /api/Paiements/especes pour un paiement en espèces.");
        }

        // --------------------------------------------------------
        // Utilisateur connecté
        // --------------------------------------------------------

        var utilisateurId = GetCurrentUserId();

        if (!utilisateurId.HasValue)
        {
            return Unauthorized(
                "Impossible d'identifier l'utilisateur connecté.");
        }

        // --------------------------------------------------------
        // Contrat du locataire connecté
        // --------------------------------------------------------

        var contrat = await _context.Contrats
            .AsNoTracking()
            .Include(c => c.Locataire)
            .Include(c => c.UniteLocative)
                .ThenInclude(u => u.BienImmobilier)
            .FirstOrDefaultAsync(c =>
                c.Id == request.ContratId &&
                c.LocataireId == utilisateurId.Value &&
                !c.EstSupprime);

        if (contrat == null)
        {
            return NotFound(
                "Contrat introuvable ou vous n'êtes pas autorisé à y accéder.");
        }

        if (contrat.Statut != StatutContrat.Actif)
        {
            return BadRequest(
                "Impossible d'effectuer un paiement pour un contrat qui n'est pas actif.");
        }

        // --------------------------------------------------------
        // Création du paiement
        // --------------------------------------------------------

        var paiement = new Paiement
        {
            ContratId = contrat.Id,

            Montant = request.Montant,

            ModePaiement = request.ModePaiement,

            StatutTransaction =
                StatutTransaction.EnAttente,

            NumeroQuittance =
                GenererNumeroQuittance()
        };

        _context.Paiements.Add(paiement);

        await _context.SaveChangesAsync();

        // --------------------------------------------------------
        // Appel du fournisseur de paiement
        // --------------------------------------------------------

        var resultat = await paiementProvider.InitierAsync(
            new InitierPaiementContext(
                PaiementId: paiement.Id,

                Montant: request.Montant,

                Telephone: request.Telephone.Trim(),

                EmailClient:
                    contrat.Locataire?.Email
                    ?? "client@mariongi.cm",

                ModePaiement:
                    request.ModePaiement,

                Description:
                    $"Loyer - Quittance {paiement.NumeroQuittance}"
            ));

        // --------------------------------------------------------
        // Échec du fournisseur
        // --------------------------------------------------------

        if (!resultat.Succes)
        {
            paiement.StatutTransaction =
                StatutTransaction.Echoue;

            await _context.SaveChangesAsync();

            return BadRequest(new
            {
                Message =
                    resultat.MessageErreur
                    ?? "Impossible d'initialiser le paiement."
            });
        }

        // --------------------------------------------------------
        // Référence opérateur
        // --------------------------------------------------------

        paiement.ReferenceTransactionOperateur =
            resultat.ReferenceExterne;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message =
                "Demande de paiement envoyée sur le mobile.",

            PaiementId =
                paiement.Id,

            NumeroQuittance =
                paiement.NumeroQuittance,

            ReferenceTransactionOperateur =
                paiement.ReferenceTransactionOperateur,

            Statut =
                paiement.StatutTransaction.ToString()
        });
    }


    // ============================================================
    // 2. ENREGISTRER UN PAIEMENT EN ESPÈCES
    //
    // ADMINISTRATEUR / GESTIONNAIRE UNIQUEMENT
    //
    // Le contrat doit appartenir à leur société.
    // ============================================================

    [HttpPost("especes")]
    [Authorize(
        Roles =
            nameof(RoleUtilisateur.Administrateur) + "," +
            nameof(RoleUtilisateur.Gestionnaire))]
    public async Task<IActionResult> EnregistrerPaiementEspeces(
        [FromBody] PaiementEspecesRequest request)
    {
        // --------------------------------------------------------
        // Validation
        // --------------------------------------------------------

        if (request.ContratId == Guid.Empty)
        {
            return BadRequest(
                "Le contrat est obligatoire.");
        }

        if (request.Montant <= 0)
        {
            return BadRequest(
                "Le montant du paiement doit être supérieur à zéro.");
        }

        // --------------------------------------------------------
        // Utilisateur connecté
        // --------------------------------------------------------

        var utilisateurId = GetCurrentUserId();

        if (!utilisateurId.HasValue)
        {
            return Unauthorized(
                "Impossible d'identifier l'utilisateur connecté.");
        }

        // --------------------------------------------------------
        // Société
        // --------------------------------------------------------

        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync();

        if (!societeId.HasValue)
        {
            return Unauthorized(
                "Impossible de déterminer la société de l'utilisateur connecté.");
        }

        // --------------------------------------------------------
        // Contrat appartenant à la société
        // --------------------------------------------------------

        var contrat = await _context.Contrats
            .Include(c => c.Locataire)
            .Include(c => c.UniteLocative)
                .ThenInclude(u => u.BienImmobilier)
            .FirstOrDefaultAsync(c =>
                c.Id == request.ContratId &&
                !c.EstSupprime &&

                c.UniteLocative != null &&
                c.UniteLocative.BienImmobilier != null &&

                c.UniteLocative
                    .BienImmobilier
                    .SocieteId == societeId.Value);

        if (contrat == null)
        {
            return NotFound(
                "Contrat introuvable ou inaccessible.");
        }

        if (contrat.Statut != StatutContrat.Actif)
        {
            return BadRequest(
                "Impossible d'enregistrer un paiement pour un contrat qui n'est pas actif.");
        }

        // --------------------------------------------------------
        // Création
        // --------------------------------------------------------

        var paiement = new Paiement
        {
            ContratId =
                contrat.Id,

            Montant =
                request.Montant,

            ModePaiement =
                ModePaiement.Especes,

            StatutTransaction =
                StatutTransaction.Confirme,

            NumeroQuittance =
                GenererNumeroQuittance(),

            EnregistreParUtilisateurId =
                utilisateurId.Value
        };

        _context.Paiements.Add(paiement);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message =
                "Paiement enregistré.",

            PaiementId =
                paiement.Id,

            NumeroQuittance =
                paiement.NumeroQuittance,

            Statut =
                paiement.StatutTransaction.ToString()
        });
    }


    // ============================================================
    // 3. CONSULTER LE STATUT D'UN PAIEMENT
    //
    // ADMIN / GESTIONNAIRE :
    //    uniquement leur société
    //
    // LOCATAIRE :
    //    uniquement son paiement
    //
    // AGENT :
    //    aucun accès
    // ============================================================

    [HttpGet("{id:guid}/statut")]
    [Authorize(
        Roles =
            nameof(RoleUtilisateur.Administrateur) + "," +
            nameof(RoleUtilisateur.Gestionnaire) + "," +
            nameof(RoleUtilisateur.Locataire))]
    public async Task<IActionResult> GetStatut(Guid id)
    {
        var paiement =
            await GetPaiementAccessibleAsync(id);

        if (paiement == null)
        {
            return NotFound(
                "Paiement introuvable ou inaccessible.");
        }

        return Ok(new
        {
            PaiementId =
                paiement.Id,

            NumeroQuittance =
                paiement.NumeroQuittance,

            Statut =
                paiement.StatutTransaction.ToString(),

            ModePaiement =
                paiement.ModePaiement.ToString(),

            Montant =
                paiement.Montant,

            DatePaiement =
                paiement.DatePaiement
        });
    }


    // ============================================================
    // 4. TÉLÉCHARGER UNE QUITTANCE
    //
    // Le paiement doit :
    //
    // 1. être accessible à l'utilisateur
    // 2. être confirmé
    // ============================================================

    [HttpGet("{id:guid}/quittance")]
    [Authorize(
        Roles =
            nameof(RoleUtilisateur.Administrateur) + "," +
            nameof(RoleUtilisateur.Gestionnaire) + "," +
            nameof(RoleUtilisateur.Locataire))]
    public async Task<IActionResult> TelechargerQuittance(Guid id)
    {
        var paiement =
            await GetPaiementAccessibleAsync(
                id,
                inclureRelations: true);

        if (paiement == null)
        {
            return NotFound(
                "Paiement introuvable ou inaccessible.");
        }

        if (paiement.StatutTransaction !=
            StatutTransaction.Confirme)
        {
            return BadRequest(
                "Impossible de générer une quittance pour un paiement non confirmé.");
        }

        var pdf =
            _pdfService.GenererQuittance(paiement);

        return File(
            pdf,
            "application/pdf",
            $"Quittance_{paiement.NumeroQuittance}.pdf");
    }


    // ============================================================
    // 5. LISTE DES PAIEMENTS
    //
    // ADMIN / GESTIONNAIRE :
    //    uniquement leur société
    //
    // LOCATAIRE :
    //    uniquement ses paiements
    //
    // AGENT :
    //    aucun accès
    // ============================================================

    [HttpGet]
    [Authorize(
        Roles =
            nameof(RoleUtilisateur.Administrateur) + "," +
            nameof(RoleUtilisateur.Gestionnaire) + "," +
            nameof(RoleUtilisateur.Locataire))]
    public async Task<IActionResult> GetPaiements()
    {
        var query = _context.Paiements
            .AsNoTracking()
            .Where(p =>
                !p.EstSupprime &&
                p.Contrat != null &&
                !p.Contrat.EstSupprime);

        // --------------------------------------------------------
        // LOCATAIRE
        // --------------------------------------------------------

        if (User.IsInRole(
                nameof(RoleUtilisateur.Locataire)))
        {
            var utilisateurId =
                GetCurrentUserId();

            if (!utilisateurId.HasValue)
            {
                return Unauthorized(
                    "Impossible d'identifier le locataire connecté.");
            }

            query = query.Where(p =>
                p.Contrat.LocataireId ==
                utilisateurId.Value);
        }

        // --------------------------------------------------------
        // ADMINISTRATEUR / GESTIONNAIRE
        // --------------------------------------------------------

        else
        {
            var societeId =
                await GetSocieteIdUtilisateurConnecteAsync();

            if (!societeId.HasValue)
            {
                return Unauthorized(
                    "Impossible de déterminer la société de l'utilisateur connecté.");
            }

            query = query.Where(p =>
                p.Contrat.UniteLocative != null &&
                p.Contrat.UniteLocative.BienImmobilier != null &&
                p.Contrat.UniteLocative
                    .BienImmobilier.SocieteId ==
                    societeId.Value);
        }

        // --------------------------------------------------------
        // PROJECTION
        // --------------------------------------------------------
        //
        // Cette projection correspond maintenant directement
        // au PaiementListItem côté Angular.
        // --------------------------------------------------------

        var paiements = await query
            .OrderByDescending(p => p.DatePaiement)
            .Select(p => new PaiementListItemDto
            {
                Id =
                    p.Id,

                DatePaiement =
                    p.DatePaiement,

                Montant =
                    p.Montant,

                Mode =
                    p.ModePaiement,

                Statut =
                    p.StatutTransaction,

                NumeroQuittance =
                    p.NumeroQuittance,

                BienNom = p.Contrat.UniteLocative.BienImmobilier.Nom,

                LocataireNom =
                    p.Contrat != null &&
                    p.Contrat.Locataire != null

                        ? $"{p.Contrat.Locataire.Nom} " +
                          $"{p.Contrat.Locataire.Prenom}"

                        : "N/A",

                UniteReference =
                    p.Contrat != null &&
                    p.Contrat.UniteLocative != null

                        ? p.Contrat
                            .UniteLocative
                            .Reference

                        : "N/A",

                BienReference =
                    p.Contrat != null &&
                    p.Contrat.UniteLocative != null &&
                    p.Contrat.UniteLocative
                        .BienImmobilier != null

                        ? p.Contrat
                            .UniteLocative
                            .BienImmobilier
                            .Reference

                        : "N/A"
            })
            .ToListAsync();

        return Ok(paiements);
    }


    // ============================================================
    // 6. TÉLÉCHARGER TOUTES LES QUITTANCES D'UN CONTRAT
    //
    // ADMIN / GESTIONNAIRE :
    //    uniquement les contrats de leur société
    //
    // LOCATAIRE :
    //    uniquement ses propres contrats
    // ============================================================

    [HttpGet(
        "contrat/{contratId:guid}/toutes-les-quittances")]
    [Authorize(
        Roles =
            nameof(RoleUtilisateur.Administrateur) + "," +
            nameof(RoleUtilisateur.Gestionnaire) + "," +
            nameof(RoleUtilisateur.Locataire))]
    public async Task<IActionResult>
        TelechargerToutesLesQuittances(
            Guid contratId)
    {
        if (contratId == Guid.Empty)
        {
            return BadRequest(
                "Le contrat est obligatoire.");
        }

        // --------------------------------------------------------
        // Vérification d'accès au contrat
        // --------------------------------------------------------

        var contratAccessible =
            await EstContratAccessibleAsync(contratId);

        if (!contratAccessible)
        {
            return NotFound(
                "Contrat introuvable ou inaccessible.");
        }

        // --------------------------------------------------------
        // Paiements confirmés du contrat
        // --------------------------------------------------------

        var paiements = await _context.Paiements
            .Include(p => p.Contrat)
                .ThenInclude(c => c.Locataire)

            .Include(p => p.Contrat)
                .ThenInclude(c => c.UniteLocative)
                    .ThenInclude(u => u.BienImmobilier)

            .Where(p =>
                p.ContratId == contratId &&

                p.StatutTransaction ==
                    StatutTransaction.Confirme &&

                !p.EstSupprime)

            .OrderBy(p => p.DatePaiement)
            .ToListAsync();

        if (paiements.Count == 0)
        {
            return NotFound(
                "Aucun paiement confirmé trouvé pour ce contrat.");
        }

        // --------------------------------------------------------
        // Création du ZIP
        // --------------------------------------------------------

        await using var memoryStream =
            new MemoryStream();

        using (var archive = new ZipArchive(
            memoryStream,
            ZipArchiveMode.Create,
            leaveOpen: true))
        {
            foreach (var paiement in paiements)
            {
                var pdfBytes =
                    _pdfService.GenererQuittance(
                        paiement);

                var entry =
                    archive.CreateEntry(
                        $"Quittance_{paiement.NumeroQuittance}.pdf");

                await using var entryStream =
                    entry.Open();

                await entryStream.WriteAsync(
                    pdfBytes.AsMemory());
            }
        }

        memoryStream.Position = 0;

        return File(
            memoryStream.ToArray(),
            "application/zip",
            $"Quittances_Contrat_{contratId}.zip");
    }


    // ============================================================
    // 7. WEBHOOK NOTCHPAY
    //
    // PUBLIC
    //
    // Protégé par signature cryptographique.
    // ============================================================

    [HttpPost("webhook/notchpay")]
    [AllowAnonymous]
    public async Task<IActionResult> WebhookNotchpay(
        [FromServices]
        IOptions<NotchpayPaiementOptions> options)
    {
        Request.EnableBuffering();

        using var reader =
            new StreamReader(
                Request.Body,
                leaveOpen: true);

        var rawBody =
            await reader.ReadToEndAsync();

        Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return BadRequest(new
            {
                error = "Payload vide."
            });
        }

        var signature =
            Request.Headers["x-notch-signature"]
                .ToString();

        var hashKey =
            options.Value.WebhookHashKey;

        if (string.IsNullOrWhiteSpace(hashKey))
        {
            return StatusCode(500, new
            {
                error =
                    "La clé de signature du webhook n'est pas configurée."
            });
        }

        if (!VerifierSignature(
                rawBody,
                signature,
                hashKey))
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    error =
                        "Invalid signature"
                });
        }

        NotchpayWebhookPayload? payload;

        try
        {
            payload =
                JsonSerializer.Deserialize
                    <NotchpayWebhookPayload>(
                        rawBody);
        }
        catch (JsonException)
        {
            return BadRequest(new
            {
                error =
                    "Payload webhook invalide."
            });
        }

        if (payload?.Data?.Reference == null)
        {
            return BadRequest(new
            {
                error =
                    "Référence de paiement absente."
            });
        }

        // --------------------------------------------------------
        // Recherche par référence opérateur
        // --------------------------------------------------------

        var paiement =
            await _context.Paiements
                .FirstOrDefaultAsync(p =>
                    p.ReferenceTransactionOperateur ==
                        payload.Data.Reference &&

                    !p.EstSupprime);

        if (paiement == null)
        {
            return NotFound(new
            {
                error =
                    "Paiement introuvable."
            });
        }

        // --------------------------------------------------------
        // Idempotence
        // --------------------------------------------------------

        if (paiement.StatutTransaction !=
            StatutTransaction.EnAttente)
        {
            return Ok(new
            {
                message =
                    "Événement déjà traité."
            });
        }

        // --------------------------------------------------------
        // Traitement de l'événement
        // --------------------------------------------------------

        switch (payload.Event)
        {
            case "payment.complete":

                paiement.StatutTransaction =
                    StatutTransaction.Confirme;

                break;

            case "payment.failed":

                paiement.StatutTransaction =
                    StatutTransaction.Echoue;

                break;

            default:

                return Ok(new
                {
                    message =
                        "Événement reçu mais non pris en charge."
                });
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message =
                "Webhook traité avec succès."
        });
    }


    // ============================================================
    // 8. VÉRIFIER SI UN CONTRAT EST ACCESSIBLE
    // ============================================================

    private async Task<bool> EstContratAccessibleAsync(
        Guid contratId)
    {
        var query =
            _context.Contrats
                .AsNoTracking()
                .Where(c =>
                    c.Id == contratId &&
                    !c.EstSupprime);

        // --------------------------------------------------------
        // LOCATAIRE
        // --------------------------------------------------------

        if (User.IsInRole(
                nameof(RoleUtilisateur.Locataire)))
        {
            var utilisateurId =
                GetCurrentUserId();

            if (!utilisateurId.HasValue)
            {
                return false;
            }

            query = query.Where(c =>
                c.LocataireId ==
                utilisateurId.Value);
        }

        // --------------------------------------------------------
        // ADMIN / GESTIONNAIRE
        // --------------------------------------------------------

        else
        {
            var societeId =
                await GetSocieteIdUtilisateurConnecteAsync();

            if (!societeId.HasValue)
            {
                return false;
            }

            query = query.Where(c =>
                c.UniteLocative != null &&
                c.UniteLocative.BienImmobilier != null &&

                c.UniteLocative
                    .BienImmobilier
                    .SocieteId ==
                    societeId.Value);
        }

        return await query.AnyAsync();
    }


    // ============================================================
    // 9. RÉCUPÉRER UN PAIEMENT ACCESSIBLE
    //
    // IMPORTANT :
    //
    // La vérification d'autorisation est effectuée dans la
    // requête SQL elle-même.
    //
    // Il ne suffit jamais de faire :
    //
    //    FirstOrDefaultAsync(p => p.Id == id)
    //
    // puis de vérifier après.
    // ============================================================

    private async Task<Paiement?>
        GetPaiementAccessibleAsync(
            Guid paiementId,
            bool inclureRelations = false)
    {
        IQueryable<Paiement> query =
            _context.Paiements;

        // --------------------------------------------------------
        // Relations nécessaires à la quittance
        // --------------------------------------------------------

        if (inclureRelations)
        {
            query = query
                .Include(p => p.Contrat)
                    .ThenInclude(c => c.Locataire)

                .Include(p => p.Contrat)
                    .ThenInclude(c => c.UniteLocative)
                        .ThenInclude(u =>
                            u.BienImmobilier);
        }
        else
        {
            query = query
                .Include(p => p.Contrat);
        }

        // --------------------------------------------------------
        // Paiement valide
        // --------------------------------------------------------

        query = query.Where(p =>
            p.Id == paiementId &&

            !p.EstSupprime &&

            p.Contrat != null &&

            !p.Contrat.EstSupprime);

        // --------------------------------------------------------
        // LOCATAIRE
        // --------------------------------------------------------

        if (User.IsInRole(
                nameof(RoleUtilisateur.Locataire)))
        {
            var utilisateurId =
                GetCurrentUserId();

            if (!utilisateurId.HasValue)
            {
                return null;
            }

            query = query.Where(p =>
                p.Contrat.LocataireId ==
                    utilisateurId.Value);
        }

        // --------------------------------------------------------
        // ADMIN / GESTIONNAIRE
        // --------------------------------------------------------

        else
        {
            var societeId =
                await GetSocieteIdUtilisateurConnecteAsync();

            if (!societeId.HasValue)
            {
                return null;
            }

            query = query.Where(p =>
                p.Contrat.UniteLocative != null &&

                p.Contrat.UniteLocative
                    .BienImmobilier != null &&

                p.Contrat.UniteLocative
                    .BienImmobilier
                    .SocieteId ==
                    societeId.Value);
        }

        return await query
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }


    // ============================================================
    // 10. SOCIÉTÉ DE L'UTILISATEUR CONNECTÉ
    //
    // Vérifie :
    //
    // - utilisateur existant
    // - non supprimé
    // - actif
    // - société associée
    // ============================================================

    private async Task<Guid?>
        GetSocieteIdUtilisateurConnecteAsync()
    {
        var currentUserId =
            GetCurrentUserId();

        if (!currentUserId.HasValue)
        {
            return null;
        }

        return await _context.Utilisateurs
            .AsNoTracking()
            .Where(u =>
                u.Id ==
                    currentUserId.Value &&

                !u.EstSupprime &&

                u.Statut)

            .Select(u =>
                (Guid?)u.SocieteId)

            .FirstOrDefaultAsync();
    }


    // ============================================================
    // 11. ID UTILISATEUR CONNECTÉ
    // ============================================================

    private Guid? GetCurrentUserId()
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (Guid.TryParse(
                userId,
                out var parsedUserId))
        {
            return parsedUserId;
        }

        return null;
    }


    // ============================================================
    // 12. VÉRIFICATION SIGNATURE WEBHOOK
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

        using var hmac =
            new HMACSHA256(
                Encoding.UTF8.GetBytes(hashKey));

        var computedBytes =
            hmac.ComputeHash(
                Encoding.UTF8.GetBytes(payload));

        var computedHex =
            Convert.ToHexString(
                computedBytes)
            .ToLowerInvariant();

        var expectedBytes =
            Encoding.UTF8.GetBytes(
                computedHex);

        var providedBytes =
            Encoding.UTF8.GetBytes(
                signature
                    .Trim()
                    .ToLowerInvariant());

        if (expectedBytes.Length !=
            providedBytes.Length)
        {
            return false;
        }

        return CryptographicOperations
            .FixedTimeEquals(
                expectedBytes,
                providedBytes);
    }


    // ============================================================
    // 13. NUMÉRO DE QUITTANCE
    // ============================================================

    private static string GenererNumeroQuittance()
    {
        return
            $"Q-{DateTime.UtcNow:yyyyMMdd}-" +
            $"{Guid.NewGuid()
                .ToString()[..4]
                .ToUpperInvariant()}";
    }
}


// =================================================================
// DTO LISTE DES PAIEMENTS
// =================================================================

public sealed class PaiementListItemDto
{
    public Guid Id { get; set; }

    public DateTime DatePaiement { get; set; }

    public decimal Montant { get; set; }

    public ModePaiement Mode { get; set; }

    public StatutTransaction Statut { get; set; }

    public string NumeroQuittance { get; set; } =
        string.Empty;

    public string LocataireNom { get; set; } =
        "N/A";

    public string UniteReference { get; set; } =
        "N/A";

    public string BienReference { get; set; } =
        "N/A";
    public string BienNom { get; set; } = "N/A";
}


// =================================================================
// REQUESTS
// =================================================================

public record InitierPaiementRequest(
    Guid ContratId,
    decimal Montant,
    ModePaiement ModePaiement,
    string Telephone);


public record PaiementEspecesRequest(
    Guid ContratId,
    decimal Montant);