namespace MarionGI.Api;

public class Permission
{
    public int Id { get; set; }
    public string Module { get; set; } = string.Empty; // Ex: "Biens", "Contrats"
    public string Code { get; set; } = string.Empty;   // Ex: "Biens.Create", "Biens.Update", "Biens.Delete"
    public string Libelle { get; set; } = string.Empty;
}