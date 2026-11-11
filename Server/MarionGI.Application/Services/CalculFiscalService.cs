using MarionGI.Application.Dtos;
using MarionGI.Application.Interfaces;
using MarionGI.Domain.Entities;
using MarionGI.Domain.Enums;

namespace MarionGI.Application;

public class CalculFiscalService : ICalculFiscalService
{
    private readonly ICalculFiscalRepository _repository;

    private const string CodeRegimeFiscal = "IRPP_FONCIER";

    private const string CodeTauxImpot = "RF_TAUX_LIBERATOIRE";

    private const string CodeTauxCac = "RF_CAC";

    public CalculFiscalService(ICalculFiscalRepository repository)
    {
        _repository = repository;
    }

    public async Task<CalculFiscalResponse> CalculerDeclarationAsync(
        Guid declarationId,
        Guid societeId,
        CancellationToken cancellationToken)
    {
        // 1. Récupérer la déclaration.

        var declaration = await _repository.ObtenirDeclarationAsync(
            declarationId,
            societeId,
            cancellationToken);

        if (declaration == null)
        {
            throw new InvalidOperationException(
                "La déclaration fiscale est introuvable.");
        }

        if (declaration.TypeImpot != TypeImpotFiscal.RevenusFonciers)
        {
            throw new InvalidOperationException(
                "Ce calcul est réservé aux déclarations de revenus fonciers.");
        }

        if (declaration.BienImmobilierId.HasValue)
        {
            throw new InvalidOperationException(
                "La déclaration de revenus fonciers doit être globale à la société.");
        }

        // 2. Vérifier l'option annuelle de déduction.

        if (!declaration.OptionDeductionFiscaleId.HasValue)
        {
            throw new InvalidOperationException(
                "Aucune option de déduction fiscale n'est associée à cette déclaration.");
        }

        var option = await _repository.ObtenirOptionDeductionAsync(
            declaration.OptionDeductionFiscaleId.Value,
            societeId,
            declaration.AnneeFiscale,
            cancellationToken);

        if (option == null)
        {
            throw new InvalidOperationException(
                "L'option de déduction fiscale est introuvable ou ne correspond pas à la société et à l'année fiscale.");
        }

        var modeDeduction = option.ModeDeduction;

        if (!Enum.IsDefined(typeof(ModeDeductionCharges), modeDeduction))
        {
            throw new InvalidOperationException(
                "Le mode de déduction fiscale est invalide.");
        }

        // 3. Récupérer les loyers effectivement encaissés.

        var loyersParTrimestre =
            await _repository.CalculerLoyersParTrimestreAsync(
                societeId,
                declaration.AnneeFiscale,
                cancellationToken);

        var totalLoyersBruts = loyersParTrimestre.Values.Sum();

        if (totalLoyersBruts < 0m)
        {
            throw new InvalidOperationException(
                "Le total des loyers encaissés ne peut pas être négatif.");
        }

        // 4. Récupérer les paramètres fiscaux applicables.

        var parametres = await _repository.ObtenirParametresFiscauxAsync(
            CodeRegimeFiscal,
            TypeImpotFiscal.RevenusFonciers,
            declaration.AnneeFiscale,
            cancellationToken);

        if (parametres == null || parametres.Count == 0)
        {
            throw new InvalidOperationException(
                $"Aucun paramètre fiscal n'est disponible pour les revenus fonciers de l'année {declaration.AnneeFiscale}.");
        }

        var parametreImpot = ObtenirParametreUnique(
            parametres,
            CodeTauxImpot);

        var parametreCac = ObtenirParametreUnique(
            parametres,
            CodeTauxCac);

        var tauxImpot = ConvertirTaux(parametreImpot);
        var tauxCac = ConvertirTaux(parametreCac);

        // 5. Calculer les charges selon l'option choisie.

        decimal chargesReellesDeclarees = 0m;
        decimal chargesReellesAdmissibles = 0m;
        decimal chargesForfaitaires = 0m;
        decimal chargesDeductibles = 0m;

        if (modeDeduction == ModeDeductionCharges.FraisReels)
        {
            var chargesRetenues =
                await _repository.ObtenirChargesRetenuesAsync(
                    declaration.Id,
                    societeId,
                    declaration.AnneeFiscale,
                    null,
                    cancellationToken);

            var chargeInvalide = chargesRetenues.Any(c =>
                (c.StatutDeductibilite !=
                    StatutDeductibilite.Deductible &&
                 c.StatutDeductibilite !=
                    StatutDeductibilite.PartiellementDeductible)
                ||
                !c.MontantDeductibleFiscalement.HasValue
                ||
                c.MontantDeductibleFiscalement.Value <= 0m
                ||
                c.MontantRetenu <= 0m
                ||
                c.MontantRetenu >
                    c.MontantDeductibleFiscalement.Value
                ||
                c.MontantTTC < 0m);

            if (chargeInvalide)
            {
                throw new InvalidOperationException(
                    "Une ou plusieurs charges présentent un statut ou un montant fiscalement admissible invalide.");
            }

            chargesReellesDeclarees =
                chargesRetenues.Sum(c => c.MontantTTC);

            chargesReellesAdmissibles =
                chargesRetenues.Sum(c => c.MontantRetenu);

            chargesDeductibles = chargesReellesAdmissibles;
        }
        else if (modeDeduction ==
                 ModeDeductionCharges.Forfait30Pourcent)
        {
            chargesForfaitaires = Math.Round(
                totalLoyersBruts * 0.30m,
                2,
                MidpointRounding.AwayFromZero);

            chargesDeductibles = chargesForfaitaires;
        }
        else
        {
            throw new InvalidOperationException(
                "Le mode de déduction fiscale n'est pas pris en charge.");
        }

        // 6. Calcul du revenu net foncier.

        var revenuNetFoncier = Math.Max(
            0m,
            totalLoyersBruts - chargesDeductibles);

        // 7. Calcul de l'impôt principal et du CAC.
        // Les taux sont stockés sous forme décimale.
        // Exemple : 0.10 représente 10 %.

        var baseImposable = totalLoyersBruts;

        var montantImpotPrincipal = Math.Round(
            baseImposable * tauxImpot,
            2,
            MidpointRounding.AwayFromZero);

        // Le CAC est calculé sur l'impôt principal.

        var montantCac = Math.Round(
            montantImpotPrincipal * tauxCac,
            2,
            MidpointRounding.AwayFromZero);

        var montantImpotTotal = Math.Round(
            montantImpotPrincipal + montantCac,
            2,
            MidpointRounding.AwayFromZero);

        // 8. Vérifier les paiements fiscaux déjà effectués.

        var acomptesVerses = declaration.AcomptesVerses ?? 0m;

        if (acomptesVerses < 0m)
        {
            throw new InvalidOperationException(
                "Le montant des paiements fiscaux déjà effectués ne peut pas être négatif.");
        }

        var soldeImpot = Math.Round(
            montantImpotTotal - acomptesVerses,
            2,
            MidpointRounding.AwayFromZero);

        // 9. Enregistrer les résultats.

        declaration.BienImmobilierId = null;

        declaration.OptionDeductionFiscaleId = option.Id;

        declaration.ModeDeduction = modeDeduction;

        declaration.TotalLoyersBruts = totalLoyersBruts;

        declaration.ChargesForfaitaires = chargesForfaitaires;

        declaration.ChargesReellesDeclarees =
            chargesReellesDeclarees;

        declaration.ChargesReellesAdmissibles =
            chargesReellesAdmissibles;

        declaration.RevenuNetFoncier = revenuNetFoncier;

        declaration.BaseImposable = baseImposable;

        // MontantImpot représente le total impôt principal + CAC.

        declaration.MontantImpot = montantImpotTotal;

        declaration.AcomptesVerses = acomptesVerses;

        declaration.SoldeImpot = soldeImpot;

        declaration.DateCalcul = DateTime.UtcNow;

        await _repository.EnregistrerAsync(cancellationToken);

        return new CalculFiscalResponse
        {
            Message = "Le calcul fiscal a été effectué avec succès.",
            Id = declaration.Id,
            AnneeFiscale = declaration.AnneeFiscale,
            ModeDeduction = declaration.ModeDeduction,

            TotalLoyersBruts = declaration.TotalLoyersBruts,
            ChargesForfaitaires = declaration.ChargesForfaitaires,
            ChargesReellesDeclarees = declaration.ChargesReellesDeclarees,
            ChargesReellesAdmissibles = declaration.ChargesReellesAdmissibles,

            RevenuNetFoncier = declaration.RevenuNetFoncier,
            BaseImposable = declaration.BaseImposable,

            MontantImpotPrincipal = montantImpotPrincipal,
            TauxCac = tauxCac,
            MontantCac = montantCac,

            MontantImpot = declaration.MontantImpot,
            AcomptesVerses = declaration.AcomptesVerses,
            SoldeImpot = declaration.SoldeImpot,

            DateCalcul = declaration.DateCalcul
        };
    }

    private static ParametreFiscal ObtenirParametreUnique(
        List<ParametreFiscal> parametres,
        string codeRegle)
    {
        var correspondances = parametres
            .Where(p => p.CodeRegle == codeRegle)
            .ToList();

        if (correspondances.Count == 0)
        {
            throw new InvalidOperationException(
                $"Le paramètre fiscal '{codeRegle}' est absent pour l'année demandée.");
        }

        if (correspondances.Count > 1)
        {
            throw new InvalidOperationException(
                $"Plusieurs paramètres fiscaux portent le code '{codeRegle}' pour l'année demandée.");
        }

        var parametre = correspondances[0];

        if (parametre.TypeParametre != TypeParametreFiscal.Taux)
        {
            throw new InvalidOperationException(
                $"Le paramètre '{codeRegle}' doit être de type Taux.");
        }

        if (parametre.TypeImpot != TypeImpotFiscal.RevenusFonciers)
        {
            throw new InvalidOperationException(
                $"Le paramètre '{codeRegle}' n'est pas associé aux revenus fonciers.");
        }

        return parametre;
    }

    private static decimal ConvertirTaux(ParametreFiscal parametre)
    {
        // Convention de MarionGI :
        // 0.10 = 10 %, 0.15 = 15 %, 0.30 = 30 %.

        if (parametre.Valeur <= 0m || parametre.Valeur > 1m)
        {
            throw new InvalidOperationException(
                $"Le taux fiscal '{parametre.CodeRegle}' doit être supérieur à 0 et inférieur ou égal à 1. Valeur reçue : {parametre.Valeur}.");
        }

        return parametre.Valeur;
    }
}
