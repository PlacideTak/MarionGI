using MarionGI.Domain;
using System.Net;
using System.Text.Json;

namespace MarionGI.Api.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur gérée par le middleware.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        // Détermination du code HTTP et du message selon le type d'exception
        var statusCode = HttpStatusCode.InternalServerError;
        var message = exception.Message;

        if (exception is CompteInactifException)
        {
            statusCode = HttpStatusCode.Forbidden; // Code 403
            // Tu peux laisser exception.Message ("Votre compte est inactif.") 
            // ou forcer un message standardisé si tu préfères
        }
        // Tu pourras facilement ajouter d'autres "else if" ici pour d'autres exceptions métier

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            Code = context.Response.StatusCode,
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}