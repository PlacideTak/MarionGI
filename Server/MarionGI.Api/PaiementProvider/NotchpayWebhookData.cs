using System.Text.Json.Serialization;

namespace MarionGI.Api.PaiementProvider;

public class NotchpayWebhookData
{
    [JsonPropertyName("reference")]
    public string? Reference { get; set; }
}