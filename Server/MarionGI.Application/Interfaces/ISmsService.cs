namespace MarionGI.Application.Services;

public interface ISmsService
{
    /// <summary>
    /// Envoie un SMS à un numéro au format international (ex: +237699001122).
    /// </summary>
    /// <param name="telephone">Numéro du destinataire</param>
    /// <param name="message">Contenu du SMS</param>
    /// <returns>True si l'envoi a réussi</returns>
    Task<bool> EnvoyerSmsAsync(string telephone, string message);
}