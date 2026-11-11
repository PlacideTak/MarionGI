using MarionGI.Application.Interfaces;
using MarionGI.Domain.Entities;
using MarionGI.Domain.Enums;
using MarionGI.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MarionGI.Persistence.Repositories;

public class CalculFiscalRepository : ICalculFiscalRepository
{
    private readonly MarionDbContext _context;

    public CalculFiscalRepository(MarionDbContext context)
    {
        _context = context;
    }


    public async Task<List<ParametreFiscal>> ObtenirParametresFiscauxAsync(
        string codeRegime,
        TypeImpotFiscal typeImpot,
        int anneeFiscale,
        CancellationToken cancellationToken)
    {
        var debutAnnee = new DateTime(anneeFiscale, 1, 1);
        var finAnnee = new DateTime(anneeFiscale, 12, 31, 23, 59, 59);

        return await _context.ParametresFiscaux
            .AsNoTracking()
            .Include(p => p.RegimeFiscal)
            .Where(p =>
                !p.EstSupprime &&
                p.AnneeFiscale == anneeFiscale &&
                p.TypeImpot == typeImpot &&
                p.DateDebutValidite <= finAnnee &&
                (!p.DateFinValidite.HasValue ||
                 p.DateFinValidite.Value >= debutAnnee) &&
                (
                    p.RegimeFiscalId == null ||
                    (
                        p.RegimeFiscal != null &&
                        !p.RegimeFiscal.EstSupprime &&
                        p.RegimeFiscal.Code == codeRegime
                    )
                ))
            .ToListAsync(cancellationToken);
    }


    public async Task<Dictionary<int, decimal>> CalculerLoyersParTrimestreAsync(
    Guid societeId,
    int anneeFiscale,
    CancellationToken cancellationToken)
    {
        var debutAnnee = new DateTime(anneeFiscale, 1, 1);
        var finAnnee = debutAnnee.AddYears(1);

        var query = _context.Paiements
            .AsNoTracking()
            .Where(p =>
                !p.EstSupprime &&
                p.StatutTransaction == StatutTransaction.Confirme &&
                p.DatePaiement >= debutAnnee &&
                p.DatePaiement < finAnnee &&
                !p.Contrat.EstSupprime &&
                !p.Contrat.UniteLocative.EstSupprime &&
                !p.Contrat.UniteLocative.BienImmobilier.EstSupprime &&
                p.Contrat.UniteLocative.BienImmobilier.SocieteId == societeId);

        var paiementSansType = await query.AnyAsync(
            p => p.TypePaiement == null,
            cancellationToken);

        if (paiementSansType)
        {
            throw new InvalidOperationException(
                "Un ou plusieurs paiements confirmés ne possèdent pas de type de paiement.");
        }

        var paiementsLoyers = query.Where(
            p => p.TypePaiement == TypePaiement.Loyer);

        var ventilationInvalide = await paiementsLoyers.AnyAsync(
            p =>
                !p.MontantLoyerEncaisse.HasValue ||
                !p.MontantChargesLocativesEncaisse.HasValue ||
                p.MontantLoyerEncaisse.Value < 0m ||
                p.MontantChargesLocativesEncaisse.Value < 0m ||
                p.MontantLoyerEncaisse.Value +
                p.MontantChargesLocativesEncaisse.Value != p.Montant,
            cancellationToken);

        if (ventilationInvalide)
        {
            throw new InvalidOperationException(
                "Un ou plusieurs paiements de loyer présentent une ventilation invalide entre loyer et charges locatives.");
        }

        var paiements = await paiementsLoyers
            .Select(p => new
            {
                p.DatePaiement,
                MontantLoyer = p.MontantLoyerEncaisse!.Value
            })
            .ToListAsync(cancellationToken);

        var resultats = paiements
            .GroupBy(p => ((p.DatePaiement.Month - 1) / 3) + 1)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(p => p.MontantLoyer));

        for (var trimestre = 1; trimestre <= 4; trimestre++)
        {
            if (!resultats.ContainsKey(trimestre))
            {
                resultats[trimestre] = 0m;
            }
        }

        return resultats;
    }

    public async Task<DeclarationFiscale?> ObtenirDeclarationAsync(
        Guid declarationId,
        Guid societeId,
        CancellationToken cancellationToken)
    {
        return await _context.DeclarationsFiscales
            .FirstOrDefaultAsync(d =>
                d.Id == declarationId &&
                d.SocieteId == societeId &&
                !d.EstSupprime,
                cancellationToken);
    }

    public async Task<OptionDeductionFiscale?> ObtenirOptionDeductionAsync(
        Guid optionId,
        Guid societeId,
        int anneeFiscale,
        CancellationToken cancellationToken)
    {
        return await _context.OptionsDeductionFiscales
            .AsNoTracking()
            .FirstOrDefaultAsync(o =>
                o.Id == optionId &&
                o.SocieteId == societeId &&
                o.AnneeFiscale == anneeFiscale &&
                !o.EstSupprime,
                cancellationToken);
    }

    public async Task<bool> BienExisteAsync(
        Guid bienImmobilierId,
        Guid societeId,
        CancellationToken cancellationToken)
    {
        return await _context.BiensImmobiliers
            .AsNoTracking()
            .AnyAsync(b =>
                b.Id == bienImmobilierId &&
                b.SocieteId == societeId &&
                !b.EstSupprime,
                cancellationToken);
    }

    // ============================================================
    // CALCULER LES LOYERS BRUTS EFFECTIVEMENT ENCAISSÉS
    // ============================================================

    public async Task<decimal> CalculerTotalLoyersAsync(
        Guid societeId,
        int anneeFiscale,
        Guid? bienImmobilierId,
        CancellationToken cancellationToken)
    {
        var debutAnnee = new DateTime(anneeFiscale, 1, 1);
        var debutAnneeSuivante = debutAnnee.AddYears(1);

        // Paiements confirmés, encaissés pendant l'année fiscale
        // et appartenant à la société concernée.

        var query = _context.Paiements
            .AsNoTracking()
            .Where(p =>
                p.StatutTransaction == StatutTransaction.Confirme &&
                p.DatePaiement >= debutAnnee &&
                p.DatePaiement < debutAnneeSuivante &&
                !p.EstSupprime &&
                !p.Contrat.EstSupprime &&
                !p.Contrat.UniteLocative.EstSupprime &&
                !p.Contrat.UniteLocative.BienImmobilier.EstSupprime &&
                p.Contrat.UniteLocative.BienImmobilier.SocieteId ==
                    societeId);

        if (bienImmobilierId.HasValue)
        {
            query = query.Where(p =>
                p.Contrat.UniteLocative.BienImmobilierId ==
                    bienImmobilierId.Value);
        }

        // Les anciens paiements sans TypePaiement ne peuvent pas
        // être automatiquement considérés comme des loyers.
        // Leur nature doit être régularisée.

        var paiementSansType = await query.AnyAsync(p =>
            p.TypePaiement == null,
            cancellationToken);

        if (paiementSansType)
        {
            throw new InvalidOperationException(
                $"Impossible de calculer les revenus fonciers de {anneeFiscale} : " +
                "un ou plusieurs paiements confirmés ne possèdent pas de type " +
                "de paiement. Ces paiements historiques doivent être régularisés.");
        }

        // Seuls les paiements de type Loyer sont contrôlés
        // et comptabilisés dans les revenus fonciers.
        //
        // Les paiements de charges locatives, de caution et autres
        // sont volontairement exclus de cette vérification.

        var paiementsLoyers = query.Where(p =>
            p.TypePaiement == TypePaiement.Loyer);

        var paiementLoyerNonVentile = await paiementsLoyers.AnyAsync(p =>
            p.MontantLoyerEncaisse == null ||
            p.MontantChargesLocativesEncaisse == null ||
            p.MontantLoyerEncaisse < 0m ||
            p.MontantChargesLocativesEncaisse < 0m ||
            p.MontantLoyerEncaisse +
                p.MontantChargesLocativesEncaisse != p.Montant,
            cancellationToken);

        if (paiementLoyerNonVentile)
        {
            throw new InvalidOperationException(
                $"Impossible de calculer les revenus fonciers de {anneeFiscale} : " +
                "un ou plusieurs paiements de loyer confirmés ne possèdent pas " +
                "une ventilation fiable. Ces paiements doivent être régularisés.");
        }

        // Additionner exclusivement les montants de loyers réellement encaissés.
        // Les versements partiels sont pris en compte.
        // Chaque paiement est comptabilisé une seule fois.

        return await paiementsLoyers.SumAsync(
            p => p.MontantLoyerEncaisse!.Value,
            cancellationToken);
    }

    // ============================================================
    // RÉCUPÉRER LES CHARGES RETENUES DANS UNE DÉCLARATION
    // ============================================================

    public async Task<List<DeclarationChargeBien>> ObtenirChargesRetenuesAsync(
        Guid declarationId,
        Guid societeId,
        int anneeFiscale,
        Guid? bienImmobilierId,
        CancellationToken cancellationToken)
    {
        var debutAnnee = new DateTime(anneeFiscale, 1, 1);
        var debutAnneeSuivante = debutAnnee.AddYears(1);

        var query = _context.DeclarationsChargesBiens
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c =>
                c.DeclarationFiscaleId == declarationId &&
                !c.EstSupprime &&
                !c.DeclarationFiscale.EstSupprime &&
                c.DeclarationFiscale.SocieteId == societeId &&
                c.DeclarationFiscale.AnneeFiscale == anneeFiscale &&
                !c.ChargeBien.EstSupprime &&
                !c.ChargeBien.BienImmobilier.EstSupprime &&
                c.ChargeBien.BienImmobilier.SocieteId == societeId &&
                c.DateDepense >= debutAnnee &&
                c.DateDepense < debutAnneeSuivante);

        // Pour une déclaration globale, aucune restriction
        // par immeuble : toutes les charges retenues de la société
        // sont récupérées.

        if (bienImmobilierId.HasValue)
        {
            query = query.Where(c =>
                c.ChargeBien.BienImmobilierId ==
                    bienImmobilierId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    // ============================================================
    // ENREGISTRER LES MODIFICATIONS
    // ============================================================

    public async Task EnregistrerAsync(
        CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
