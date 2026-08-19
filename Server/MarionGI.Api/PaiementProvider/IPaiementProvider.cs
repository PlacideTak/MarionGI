using MarionGI.Domain.Enums;

namespace MarionGI.Api.PaiementProvider;

public interface IPaiementProvider
{
    Task<InitierPaiementResult> InitierAsync(InitierPaiementContext context, CancellationToken ct = default);
}

public record InitierPaiementContext(
    Guid PaiementId,
    decimal Montant,
    string Telephone,
    string EmailClient,
    ModePaiement ModePaiement,
    string Description
);

public record InitierPaiementResult(bool Succes, string? ReferenceExterne, string? MessageErreur);
