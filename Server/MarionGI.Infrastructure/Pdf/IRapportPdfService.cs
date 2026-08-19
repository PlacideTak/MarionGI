using System.Text.Json;

namespace MarionGI.Infrastructure.Pdf;

public interface IRapportPdfService
{
    byte[] GenererRapportPdf(string typeRapport, string titreRapport, JsonElement donneesBrutes);
}
