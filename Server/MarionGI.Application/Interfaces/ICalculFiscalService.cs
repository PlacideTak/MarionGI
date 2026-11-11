using MarionGI.Application.Dtos;

namespace MarionGI.Application.Interfaces;

public interface ICalculFiscalService
{
    Task<CalculFiscalResponse> CalculerDeclarationAsync(
        Guid declarationId,
        Guid societeId,
        CancellationToken cancellationToken);
}