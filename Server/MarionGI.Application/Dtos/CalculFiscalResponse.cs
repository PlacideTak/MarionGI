using MarionGI.Domain.Enums;

namespace MarionGI.Application.Dtos;

public class CalculFiscalResponse
{
    public string Message { get; set; } = string.Empty;

    public Guid Id { get; set; }
    public int AnneeFiscale { get; set; }

    public TypeImpotFiscal TypeImpot { get; set; }
    public Guid? BienImmobilierId { get; set; }

    public ModeDeductionCharges? ModeDeduction { get; set; }

    public decimal? TotalLoyersBruts { get; set; }
    public decimal? ChargesForfaitaires { get; set; }
    public decimal? ChargesReellesDeclarees { get; set; }
    public decimal? ChargesReellesAdmissibles { get; set; }

    public decimal? RevenuNetFoncier { get; set; }
    public decimal? BaseImposable { get; set; }

    public decimal? MontantImpotPrincipal { get; set; }
    public decimal? TauxCac { get; set; }
    public decimal? MontantCac { get; set; }

    public decimal MontantImpot { get; set; }
    public decimal? AcomptesVerses { get; set; }
    public decimal? SoldeImpot { get; set; }

    public DateTime? DateCalcul { get; set; }
}