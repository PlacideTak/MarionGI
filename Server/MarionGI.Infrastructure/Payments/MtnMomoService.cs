namespace MarionGI.Infrastructure.Payments;

public class MtnMomoService : IPaiementMobileService
{
    private readonly HttpClient _httpClient;

    public MtnMomoService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PaymentInitiationResponse> IniterPaiementAsync(string telephone, decimal montant, string referencePaiement)
    {
        // Simulation Push USSD MTN MoMo Cameroun (*126#)
        await Task.Delay(500);
        return new PaymentInitiationResponse(true, $"MTN-CM-{Guid.NewGuid().ToString()[..8].ToUpper()}", "Demande de paiement MTN Mobile Money envoyée.");
    }
}
