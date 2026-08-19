using MarionGI.Api.PaiementProvider;
using MarionGI.Domain.Entities;
using MarionGI.Domain.Enums;
using MarionGI.Infrastructure.Pdf;
using MarionGI.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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

    public PaiementsController(MarionDbContext context, IQuittancePdfService pdfService)
    {
        _context = context;
        _pdfService = pdfService;
    }

    [HttpPost("initier")]
    public async Task<IActionResult> InitierPaiement(
     [FromBody] InitierPaiementRequest request,
     [FromServices] IPaiementProvider paiementProvider)
    {
        if (request.ModePaiement == ModePaiement.Especes)
            return BadRequest("Utilisez /api/Paiements/especes pour un paiement en espèces.");

        var contrat = await _context.Contrats.FindAsync(request.ContratId);
        if (contrat == null) return NotFound("Contrat inexistant.");

        var paiement = new Paiement
        {
            ContratId = request.ContratId,
            Montant = request.Montant,
            ModePaiement = request.ModePaiement,
            StatutTransaction = StatutTransaction.EnAttente,
            NumeroQuittance = GenererNumeroQuittance()
        };
        _context.Paiements.Add(paiement);
        await _context.SaveChangesAsync();

        var resultat = await paiementProvider.InitierAsync(new InitierPaiementContext(
            PaiementId: paiement.Id,
            Montant: request.Montant,
            Telephone: request.Telephone,
            EmailClient: contrat.Locataire?.Email ?? "client@mariongi.cm", // ⚠️ adapter selon votre modèle Locataire
            ModePaiement: request.ModePaiement,
            Description: $"Loyer - Quittance {paiement.NumeroQuittance}"
        ));

        if (!resultat.Succes)
        {
            paiement.StatutTransaction = StatutTransaction.Echoue;
            await _context.SaveChangesAsync();
            return BadRequest(new { Message = resultat.MessageErreur });
        }

        paiement.ReferenceTransactionOperateur = resultat.ReferenceExterne;
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Demande de paiement envoyée sur le mobile.", PaiementId = paiement.Id });
    }

    // ===== Espèces : confirmation immédiate =====
    [HttpPost("especes")]
    public async Task<IActionResult> EnregistrerPaiementEspeces([FromBody] PaiementEspecesRequest request)
    {
        var contrat = await _context.Contrats.FindAsync(request.ContratId);
        if (contrat == null) return NotFound("Contrat inexistant.");

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var paiement = new Paiement
        {
            ContratId = request.ContratId,
            Montant = request.Montant,
            ModePaiement = ModePaiement.Especes,
            StatutTransaction = StatutTransaction.Confirme,
            NumeroQuittance = GenererNumeroQuittance(),
            EnregistreParUtilisateurId = userId != null ? Guid.Parse(userId) : null
        };
        _context.Paiements.Add(paiement);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Paiement enregistré.", PaiementId = paiement.Id });
    }

    // ===== Polling : suivi du statut (mobile money en attente) =====
    [HttpGet("{id}/statut")]
    [Authorize]
    public async Task<IActionResult> GetStatut(Guid id)
    {
        var paiement = await _context.Paiements.FindAsync(id);
        if (paiement == null) return NotFound();
        return Ok(new { statut = paiement.StatutTransaction.ToString() });
    }

    // ===== Quittance PDF =====
    [HttpGet("{id}/quittance")]
    public async Task<IActionResult> TelechargerQuittance(Guid id)
    {
        var paiement = await _context.Paiements
            .Include(p => p.Contrat).ThenInclude(c => c.Locataire)
            .Include(p => p.Contrat).ThenInclude(c => c.Bien)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (paiement == null) return NotFound();
        if (paiement.StatutTransaction != StatutTransaction.Confirme)
            return BadRequest("Impossible de générer une quittance pour un paiement non confirmé.");

        var pdf = _pdfService.GenererQuittance(paiement);
        return File(pdf, "application/pdf", $"Quittance_{paiement.NumeroQuittance}.pdf");
    }


    [HttpGet("Paiements")]
    public async Task<IActionResult> GetPaiements()
    {
        var paiements = await _context.Paiements
            .Include(p => p.Contrat)
                .ThenInclude(c => c.Locataire)
            .Include(p => p.Contrat)
                .ThenInclude(c => c.Bien)
            .Where(p => !p.EstSupprime)
            .OrderByDescending(p => p.DatePaiement)
            .Select(p => new
            {
                p.Id,
                p.DatePaiement,
                p.Montant,
                Mode = p.ModePaiement,
                Statut = p.StatutTransaction,
                p.NumeroQuittance,
                LocataireNom = p.Contrat != null && p.Contrat.Locataire != null
                    ? $"{p.Contrat.Locataire.Nom} {p.Contrat.Locataire.Prenom}"
                    : "N/A",
                BienReference = p.Contrat != null && p.Contrat.Bien != null
                    ? p.Contrat.Bien.Reference
                    : "N/A"
            })
            .ToListAsync();

        return Ok(paiements);
    }

    [HttpGet("contrat/{contratId}/toutes-les-quittances")]
    public async Task<IActionResult> TelechargerToutesLesQuittances(Guid contratId)
    {
        var paiements = await _context.Paiements
            .Include(p => p.Contrat).ThenInclude(c => c.Locataire)
            .Include(p => p.Contrat).ThenInclude(c => c.Bien)
            .Where(p => p.ContratId == contratId && p.StatutTransaction == StatutTransaction.Confirme && !p.EstSupprime)
            .ToListAsync();

        if (!paiements.Any()) return NotFound("Aucun paiement confirmé trouvé pour ce contrat.");

        using var memoryStream = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            foreach (var paiement in paiements)
            {
                var pdfBytes = _pdfService.GenererQuittance(paiement);
                var entry = archive.CreateEntry($"Quittance_{paiement.NumeroQuittance}.pdf");
                using var entryStream = entry.Open();
                entryStream.Write(pdfBytes, 0, pdfBytes.Length);
            }
        }

        memoryStream.Position = 0;
        return File(memoryStream.ToArray(), "application/zip", $"Quittances_Contrat_{contratId}.zip");
    }

    [HttpPost("webhook/notchpay")]
    [AllowAnonymous]
    public async Task<IActionResult> WebhookNotchpay([FromServices] IOptions<NotchpayPaiementOptions> options)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        var signature = Request.Headers["x-notch-signature"].ToString();
        var hashKey = options.Value.WebhookHashKey;

        if (!VerifierSignature(rawBody, signature, hashKey))
            return StatusCode(403, new { error = "Invalid signature" });

        var payload = JsonSerializer.Deserialize<NotchpayWebhookPayload>(rawBody);
        if (payload?.Data?.Reference == null) return BadRequest();

        var paiement = await _context.Paiements
            .FirstOrDefaultAsync(p => p.ReferenceTransactionOperateur == payload.Data.Reference);
        if (paiement == null) return NotFound();

        if (paiement.StatutTransaction != StatutTransaction.EnAttente)
            return Ok(); // idempotence

        paiement.StatutTransaction = payload.Event switch
        {
            "payment.complete" => StatutTransaction.Confirme,
            "payment.failed" => StatutTransaction.Echoue,
            _ => paiement.StatutTransaction
        };
        await _context.SaveChangesAsync();

        return Ok();
    }

    private static bool VerifierSignature(string payload, string signature, string hashKey)
    {
        if (string.IsNullOrEmpty(signature)) return false;

        var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(hashKey));
        var computedBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedHex = Convert.ToHexString(computedBytes).ToLowerInvariant();

        // Comparaison en temps constant, comme recommandé par la doc officielle
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHex),
            Encoding.UTF8.GetBytes(signature)
        );
    }

    private static string GenererNumeroQuittance() =>
        $"Q-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
}

public record InitierPaiementRequest(Guid ContratId, decimal Montant, ModePaiement ModePaiement, string Telephone);
public record PaiementEspecesRequest(Guid ContratId, decimal Montant);