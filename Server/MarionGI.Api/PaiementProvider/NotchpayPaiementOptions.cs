namespace MarionGI.Api.PaiementProvider;

public class NotchpayPaiementOptions
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookHashKey { get; set; } = string.Empty;
    public Dictionary<string, string> CanauxParMode { get; set; } = new();
}