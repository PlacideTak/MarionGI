namespace MarionGI.Application.Dtos;

public class ReinitialiserMotDePasseRequestDto
{
    public string Identifiant { get; set; } = string.Empty;

    public string CodeOtp { get; set; } = string.Empty;

    public string NouveauMotDePasse { get; set; } = string.Empty;
}
