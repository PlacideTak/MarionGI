using MarionGI.Domain.Enums;

namespace MarionGI.Application.Dtos;

public class DeclarationFiscaleRequest
{
    public Guid Id { get; set; }

    public Guid? BienImmobilierId { get; set; }

    public int AnneeFiscale { get; set; }

    public TypeImpotFiscal TypeImpot { get; set; }

    public decimal? BaseImposable { get; set; }

    public decimal MontantImpot { get; set; }

    public DateTime? DateEcheance { get; set; }

    public DateTime? DateDeclaration { get; set; }

    public string? ReferenceDeclaration { get; set; }

    public string? Observations { get; set; }

    public StatutDeclarationFiscale Statut { get; set; }
}
