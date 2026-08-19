using MarionGI.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

[Route("api/[controller]")]
[ApiController]
public class ParametresController : ControllerBase
{
    private readonly string _filePath;

    // Injection de l'environnement pour cibler proprement wwwroot
    public ParametresController(IWebHostEnvironment env)
    {
        // Si wwwroot n'existe pas encore formellement dans le contexte, on peut s'assurer qu'il est ciblé :
        string webRootPath = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");

        // Crée le dossier wwwroot s'il n'existe pas encore par sécurité
        if (!Directory.Exists(webRootPath))
        {
            Directory.CreateDirectory(webRootPath);
        }

        // Le fichier sera donc toujours : /wwwroot/pdfSettings.json
        _filePath = Path.Combine(webRootPath, "MarionGISettings.json");
    }

    [HttpGet]
    public async Task<IActionResult> GetParametres()
    {
        Console.WriteLine($"[API] Lecture du fichier dans wwwroot : {_filePath}");

        if (!System.IO.File.Exists(_filePath))
        {
            var defaultSettings = new ParametresDto();
            return Ok(defaultSettings);
        }

        var jsonContent = await System.IO.File.ReadAllTextAsync(_filePath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var settings = JsonSerializer.Deserialize<ParametresDto>(jsonContent, options);

        return Ok(settings ?? new ParametresDto());
    }

    [HttpPost]
    [Authorize(Policy = "GestionnaireOrAdmin")]
    public async Task<IActionResult> EnregistrerParametres([FromBody] ParametresDto newSettings)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var jsonString = JsonSerializer.Serialize(newSettings, options);

            // Écriture forcée dans wwwroot/pdfSettings.json
            await System.IO.File.WriteAllTextAsync(_filePath, jsonString);

            return Ok(new { message = "Paramètres enregistrés avec succès dans wwwroot !" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erreur lors de l'enregistrement", error = ex.Message });
        }
    }
}