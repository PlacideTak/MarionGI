using System.Linq.Expressions;
using System.Security.Claims;

namespace MarionGI.Api;

public static class DataQueryExtensions
{
    public static IQueryable<T> ApplyOwnershipFilter<T>(
        this IQueryable<T> query,
        ClaimsPrincipal user,
        Expression<Func<T, Guid>> ownerIdSelector) where T : class
    {
        var userRole = user.FindFirstValue(ClaimTypes.Role);
        var currentUserId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Administrateur et Gestionnaire voient l'intégralité du parc
        if (userRole is "Administrateur" or "Gestionnaire")
        {
            return query;
        }

        // Les autres rôles sont restreints à leurs propres enregistrements
        var parameter = ownerIdSelector.Parameters[0];
        var equals = Expression.Equal(
            ownerIdSelector.Body,
            Expression.Constant(currentUserId)
        );
        var lambda = Expression.Lambda<Func<T, bool>>(equals, parameter);

        return query.Where(lambda);
    }
}