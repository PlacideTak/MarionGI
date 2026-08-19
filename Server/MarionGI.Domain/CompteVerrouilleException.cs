namespace MarionGI.Api;

public class CompteVerrouilleException(int minutes) : Exception($"Compte temporairement verrouillé. Réessayez dans {minutes} minute(s).")
{
}
