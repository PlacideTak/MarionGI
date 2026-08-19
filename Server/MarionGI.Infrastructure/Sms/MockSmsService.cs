using MarionGI.Application.Services;
using Microsoft.Extensions.Logging;

namespace MarionGI.Infrastructure.Sms;

public class MockSmsService : ISmsService
{
    private readonly ILogger<MockSmsService> _logger;

    public MockSmsService(ILogger<MockSmsService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> EnvoyerSmsAsync(string telephone, string message)
    {
        // Simulation d'une latence réseau (appel d'API tiers SMS)
        await Task.Delay(200);

        _logger.LogInformation("==================================================");
        _logger.LogInformation("[MOCK SMS PROVIDER CAMEROUN]");
        _logger.LogInformation("Destinataire : {Telephone}", telephone);
        _logger.LogInformation("Message      : {Message}", message);
        _logger.LogInformation("==================================================");

        return true;
    }
}