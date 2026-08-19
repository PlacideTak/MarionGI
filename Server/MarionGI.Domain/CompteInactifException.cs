namespace MarionGI.Domain;

public class CompteInactifException : Exception
{
    public CompteInactifException() : base("Votre compte est inactif.") { }
}
