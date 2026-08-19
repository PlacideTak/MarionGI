namespace MarionGI.Infrastructure.Payments;

public interface IPaiementMobileService
{
    Task<PaymentInitiationResponse> IniterPaiementAsync(string telephone, decimal montant, string referencePaiement);
}
