using MarionGI.Domain;
using Microsoft.AspNetCore.Diagnostics;

namespace MarionGI.Api;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, message) = MapException(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            // On logue le détail réel uniquement côté serveur.
            _logger.LogError(exception, "Erreur non gérée sur {Path}", httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new { message },
            cancellationToken);

        return true; // true = exception gérée, ne pas la propager plus loin
    }

    private static (int StatusCode, string Message) MapException(Exception exception) =>
        exception switch
        {
            CompteVerrouilleException ex => (StatusCodes.Status423Locked, ex.Message),
            CompteInactifException ex => (StatusCodes.Status403Forbidden, ex.Message),
            IdentifiantsInvalidesException ex => (StatusCodes.Status401Unauthorized, ex.Message),
            // Ajoute ici d'autres exceptions métier au fur et à mesure :
            // ValidationException ex => (StatusCodes.Status400BadRequest, ex.Message),
            // RessourceNonTrouveeException ex => (StatusCodes.Status404NotFound, ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "Une erreur est survenue. Réessaie.")
        };
}