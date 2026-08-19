using System.Text.Json.Serialization;

namespace MarionGI.Api.PaiementProvider;

public class NotchpayInitResponse
{
    [JsonPropertyName("transaction")]
    public NotchpayTransaction? Transaction { get; set; }

    [JsonPropertyName("authorization_url")]
    public string? AuthorizationUrl { get; set; }
}
