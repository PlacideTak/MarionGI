using System.Text.Json.Serialization;

namespace MarionGI.Api.PaiementProvider;

public class NotchpayWebhookPayload
{
    [JsonPropertyName("event")]
    public string? Event { get; set; }

    [JsonPropertyName("data")]
    public NotchpayWebhookData? Data { get; set; }   // 👈 référence l'AUTRE classe, pas elle-même
}