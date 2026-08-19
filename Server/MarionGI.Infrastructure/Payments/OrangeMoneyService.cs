namespace MarionGI.Infrastructure.Payments;

public class OrangeMoneyService : IPaiementMobileService
{
    private readonly HttpClient _httpClient;

    public OrangeMoneyService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PaymentInitiationResponse> IniterPaiementAsync(string telephone, decimal montant, string referencePaiement)
    {
        // Simulation Push USSD Orange Money Cameroun (*144#)
        await Task.Delay(500); // Latence API
        return new PaymentInitiationResponse(true, $"OM-CM-{Guid.NewGuid().ToString()[..8].ToUpper()}", "Demande de paiement Orange Money envoyée.");
    }
}
