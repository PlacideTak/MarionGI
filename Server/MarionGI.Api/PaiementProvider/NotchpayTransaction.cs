using System.Text.Json.Serialization;

namespace MarionGI.Api.PaiementProvider;

public class NotchpayTransaction
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
