using MarionGI.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MarionGI.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ParametresController : ControllerBase
{
    private readonly string _filePath;

    public ParametresController(IWebHostEnvironment env)
    {
        // --------------------------------------------------------
        // Détermination du dossier wwwroot
        // --------------------------------------------------------
        string webRootPath =
            env.WebRootPath ??
            Path.Combine(env.ContentRootPath, "wwwroot");

        // Création du dossier si nécessaire
        if (!Directory.Exists(webRootPath))
        {
            Directory.CreateDirectory(webRootPath);
        }

        // Fichier de configuration
        _filePath = Path.Combine(
            webRootPath,
            "MarionGISettings.json");
    }

    // ============================================================
    // GET : Récupérer les paramètres
    // Administrateur + Gestionnaire
    // ============================================================
    [HttpGet]
    [Authorize(Policy = "GestionnaireOrAdmin")]
    public async Task<IActionResult> GetParametres(
        CancellationToken cancellationToken)
    {
        try
        {
            if (!System.IO.File.Exists(_filePath))
            {
                return Ok(new ParametresDto());
            }

            var jsonContent =
                await System.IO.File.ReadAllTextAsync(
                    _filePath,
                    cancellationToken);

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return Ok(new ParametresDto());
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var settings =
                JsonSerializer.Deserialize<ParametresDto>(
                    jsonContent,
                    options);

            return Ok(settings ?? new ParametresDto());
        }
        catch (JsonException)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "Le fichier de paramètres contient un JSON invalide."
                });
        }
        catch (IOException)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "Impossible de lire le fichier de paramètres."
                });
        }
    }

    // ============================================================
    // POST : Enregistrer les paramètres
    // Administrateur + Gestionnaire
    // ============================================================
    [HttpPost]
    [Authorize(Policy = "GestionnaireOrAdmin")]
    public async Task<IActionResult> EnregistrerParametres(
        [FromBody] ParametresDto newSettings,
        CancellationToken cancellationToken)
    {
        if (newSettings == null)
        {
            return BadRequest(
                new
                {
                    message = "Les paramètres sont obligatoires."
                });
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var jsonString =
                JsonSerializer.Serialize(
                    newSettings,
                    options);

            await System.IO.File.WriteAllTextAsync(
                _filePath,
                jsonString,
                cancellationToken);

            return Ok(
                new
                {
                    message = "Paramètres enregistrés avec succès."
                });
        }
        catch (JsonException)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "Impossible de sérialiser les paramètres."
                });
        }
        catch (IOException)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "Impossible d'enregistrer le fichier de paramètres."
                });
        }
    }
}