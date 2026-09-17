namespace MarionGI.Api;

public enum ScopeVisibilite
{
    Self = 1, // Données propres à l'utilisateur (Locataire, Propriétaire)
    All = 2   // Toutes les données (Admin, Gestionnaire)
}