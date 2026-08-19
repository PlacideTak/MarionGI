namespace MarionGI.Application.Dtos;

public record AuthResponseDto(bool RequisOtp, string ? AccessToken, string? RefreshToken, DateTime? Expiration, string? Telephone = null, string? CodeParSms = null);
