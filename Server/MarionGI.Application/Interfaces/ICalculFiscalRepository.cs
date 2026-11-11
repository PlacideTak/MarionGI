using MarionGI.Domain.Entities;
using MarionGI.Domain.Enums;

namespace MarionGI.Application.Interfaces;

public interface ICalculFiscalRepository
{
    Task<DeclarationFiscale?> ObtenirDeclarationAsync(
        Guid declarationId,
        Guid societeId,
        CancellationToken cancellationToken);

    Task<OptionDeductionFiscale?> ObtenirOptionDeductionAsync(
        Guid optionId,
        Guid societeId,
        int anneeFiscale,
        CancellationToken cancellationToken);

    Task<bool> BienExisteAsync(
        Guid bienImmobilierId,
        Guid societeId,
        CancellationToken cancellationToken);

    Task<decimal> CalculerTotalLoyersAsync(
        Guid societeId,
        int anneeFiscale,
        Guid? bienImmobilierId,
        CancellationToken cancellationToken);

    Task<List<DeclarationChargeBien>> ObtenirChargesRetenuesAsync(
        Guid declarationId,
        Guid societeId,
        int anneeFiscale,
        Guid? bienImmobilierId,
        CancellationToken cancellationToken);

    Task EnregistrerAsync(
        CancellationToken cancellationToken);

    Task<Dictionary<int, decimal>> CalculerLoyersParTrimestreAsync(
    Guid societeId,
    int anneeFiscale,
    CancellationToken cancellationToken);

    Task<List<ParametreFiscal>> ObtenirParametresFiscauxAsync(
        string codeRegime,
        TypeImpotFiscal typeImpot,
        int anneeFiscale,
        CancellationToken cancellationToken);
}
