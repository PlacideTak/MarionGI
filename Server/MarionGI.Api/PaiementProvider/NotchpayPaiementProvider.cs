using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MarionGI.Api.PaiementProvider;

public class NotchpayPaiementProvider : IPaiementProvider
{
    private readonly HttpClient _http;
    private readonly NotchpayPaiementOptions _options;
    private readonly ILogger<NotchpayPaiementProvider> _logger;

    public NotchpayPaiementProvider(HttpClient http, IOptions<NotchpayPaiementOptions> options, ILogger<NotchpayPaiementProvider> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _http.BaseAddress = new Uri(_options.BaseUrl);
        _http.DefaultRequestHeaders.Add("Authorization", _options.PublicKey);
        _http.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<InitierPaiementResult> InitierAsync(InitierPaiementContext context, CancellationToken ct = default)
    {
        var channel = _options.CanauxParMode.GetValueOrDefault(context.ModePaiement.ToString())
            ?? throw new InvalidOperationException($"Aucun canal Notchpay configuré pour {context.ModePaiement}.");

        var initPayload = new
        {
            amount = context.Montant,
            currency = "XAF",
            email = context.EmailClient,   // 👈 ajout recommandé
            phone = context.Telephone,
            description = context.Description,
            reference = context.PaiementId.ToString()
        };

        var initResponse = await _http.PostAsJsonAsync("/payments", initPayload, ct);
        var initBody = await initResponse.Content.ReadAsStringAsync(ct);

        if (!initResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Échec initialisation Notchpay ({Status}): {Body}", initResponse.StatusCode, initBody);
            return new InitierPaiementResult(false, null, "Échec de l'initialisation du paiement.");
        }

        var initResult = JsonSerializer.Deserialize<NotchpayInitResponse>(initBody);
        var reference = initResult?.Transaction?.Reference;
        if (reference == null)
            return new InitierPaiementResult(false, null, "Réponse Notchpay invalide (référence manquante).");

        var processPayload = new
        {
            channel,
            data = new { phone = context.Telephone }
        };

        var processResponse = await _http.PostAsJsonAsync($"/payments/{reference}", processPayload, ct);
        var processBody = await processResponse.Content.ReadAsStringAsync(ct);

        if (!processResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Échec traitement Notchpay ({Status}): {Body}", processResponse.StatusCode, processBody);
            return new InitierPaiementResult(false, reference, "Échec du déclenchement du push USSD.");
        }

        return new InitierPaiementResult(true, reference, null);
    }
}