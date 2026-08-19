namespace MarionGI.Infrastructure.Payments;

public record PaymentInitiationResponse(bool Succes, string TransactionRef, string Message);
