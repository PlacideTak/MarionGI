using System.Text.Json;

namespace MarionGI.Application.Dtos;

public class ExportRapportPdfRequest
{
    public string TypeRapport { get; set; } = string.Empty;

    public string TitreRapport { get; set; } = string.Empty;

    public JsonElement Donnees { get; set; }
}
