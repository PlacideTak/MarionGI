using MarionGI.Domain.Entities;

namespace MarionGI.Infrastructure.Pdf;

public interface IQuittancePdfService
{
    byte[] GenererQuittance(Paiement paiement);
}
