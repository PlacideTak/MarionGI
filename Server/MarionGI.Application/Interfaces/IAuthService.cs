using MarionGI.Application.Dtos;

namespace MarionGI.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> ConnexionAsync(ConnexionRequestDto dto);
    Task<AuthResponseDto> ValiderOtpAsync(ValiderOtpRequestDto dto);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto);
    
    Task DemanderRecuperationAsync(MotDePasseOublieRequestDto dto);
    Task ReinitialiserMotDePasseAsync(ReinitialiserMotDePasseRequestDto dto);
}
