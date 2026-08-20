using MarionGI.Api;
using MarionGI.Application.Dtos;
using MarionGI.Application.Interfaces;
using MarionGI.Application.Services;
using MarionGI.Domain;
using MarionGI.Domain.Entities;
using MarionGI.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MarionGI.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly MarionDbContext _context;
    private readonly IConfiguration _config;
    private readonly ISmsService _smsService;

    public AuthService(MarionDbContext context, IConfiguration config, ISmsService smsService)
    {
        _context = context;
        _config = config;
        _smsService = smsService;
    }

    public async Task<AuthResponseDto> ConnexionAsync(ConnexionRequestDto dto)
    {
        // 1. Recherche de l'utilisateur
        var user = await _context.Utilisateurs
            .FirstOrDefaultAsync(u => u.Email == dto.Identifiant || u.Telephone == dto.Identifiant) ?? throw new IdentifiantsInvalidesException();

        if(!user.Statut)
        {
            throw new CompteInactifException();
        }

        // 2. Vérifier si le compte est actuellement verrouillé
        if (user.VerrouilleJusquA.HasValue && user.VerrouilleJusquA.Value > DateTime.UtcNow)
        {
            var minutesRestantes = (int)Math.Ceiling((user.VerrouilleJusquA.Value - DateTime.UtcNow).TotalMinutes);
            throw new CompteVerrouilleException(minutesRestantes);
        }

        // 3. Vérification de la validité du format du Hash BCrypt
        bool isHashValide = !string.IsNullOrWhiteSpace(user.MotDePasseHash)
                           && user.MotDePasseHash.StartsWith("$2");

        // 4. Vérification du mot de passe avec garde-fou contre BCrypt.Verify
        bool motDePasseCorrect = isHashValide && BCrypt.Net.BCrypt.Verify(dto.MotDePasse, user.MotDePasseHash);
        if (!motDePasseCorrect)
        {
            user.TentativesConnexionEchouees++;

            if (user.TentativesConnexionEchouees >= 5)
            {
                user.VerrouilleJusquA = DateTime.UtcNow.AddMinutes(15);
                await _context.SaveChangesAsync();
                throw new CompteVerrouilleException(15);
            }

            await _context.SaveChangesAsync();
            throw new IdentifiantsInvalidesException();
        }

        // 5. Génération du code OTP 2FA (Réussite de l'étape 1)
        var otpCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        user.OtpSecret = BCrypt.Net.BCrypt.HashPassword(otpCode);
        user.OtpExpiration = DateTime.UtcNow.AddMinutes(5);
        user.TentativesConnexionEchouees = 0; // Réinitialisation des échecs
        user.VerrouilleJusquA = null; // Libération du verrou s'il était expiré
        await _context.SaveChangesAsync();

        // 6. Envoi du SMS
        await _smsService.EnvoyerSmsAsync(user.Telephone, $"Votre code de confirmation MarionGI est : {otpCode}");

        return new AuthResponseDto(
            RequisOtp: true,
            AccessToken: null,
            RefreshToken: null,
            Expiration: null,
            Telephone: user.Telephone,
            CodeParSms: otpCode
        );
    }

    public async Task<AuthResponseDto> ValiderOtpAsync(ValiderOtpRequestDto dto)
    {
        // Normalisation ou recherche sécurisée
        var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Telephone == dto.Telephone);

        if (user == null)
            throw new Exception("Utilisateur introuvable.");

        if (user.OtpExpiration < DateTime.UtcNow)
            throw new Exception("Le code OTP a expiré.");

        if (string.IsNullOrEmpty(user.OtpSecret))
            throw new Exception("Aucun OTP actif pour cet utilisateur.");

        // Vérification sécurisée avec BCrypt
        bool isOtpValid = false;
        try
        {
            isOtpValid = BCrypt.Net.BCrypt.Verify(dto.CodeOtp, user.OtpSecret);
        }
        catch
        {
            // Si le hash en base est corrompu ou mal formé, on évite le crash 500
            throw new Exception("Erreur lors de la validation du code de sécurité.");
        }

        if (!isOtpValid)
            throw new Exception("Code OTP invalide.");

        // Nettoyage de l'OTP après succès
        user.OtpSecret = null;
        user.OtpExpiration = null;
        user.DerniereConnexion = DateTime.UtcNow;

        var jwtToken = GenererJwtToken(user);
        var refreshToken = GenererRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            UtilisateurId = user.Id,
            Token = refreshToken,
            DateExpiration = DateTime.UtcNow.AddDays(7)
        });

        await _context.SaveChangesAsync();

        return new AuthResponseDto(
            RequisOtp: false,
            AccessToken: jwtToken,
            RefreshToken: refreshToken,
            Expiration: DateTime.UtcNow.AddHours(2)
        );
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        var principal = GetPrincipalFromExpiredToken(dto.AccessToken);
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new Exception("Token invalide : claim 'NameIdentifier' introuvable.");
        }

        var userId = Guid.Parse(userIdClaim);

        var tokenEnBase = await _context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == dto.RefreshToken && r.UtilisateurId == userId);

        if (tokenEnBase == null || tokenEnBase.EstRevoque || tokenEnBase.DateExpiration < DateTime.UtcNow)
            throw new Exception("Refresh Token non valide.");

        tokenEnBase.EstRevoque = true;

        var user = await _context.Utilisateurs.FindAsync(userId);
        var nouveauJwt = GenererJwtToken(user!);
        var nouveauRefreshToken = GenererRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            UtilisateurId = user!.Id,
            Token = nouveauRefreshToken,
            DateExpiration = DateTime.UtcNow.AddDays(7)
        });

        await _context.SaveChangesAsync();

        return new AuthResponseDto(false, nouveauJwt, nouveauRefreshToken, DateTime.UtcNow.AddHours(2));
    }

    private string GenererJwtToken(Utilisateur user)
    {
        var secretKey = _config["Jwt:SecretKey"]?.Trim();
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("La clé secrète JWT est introuvable.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim("email", user.Email),
        new Claim(ClaimTypes.Role, user.Role.ToString()),
        new Claim("role", user.Role.ToString()),
        new Claim("given_name", user.Prenom ?? ""),
        new Claim("family_name", user.Nom ?? ""),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

        var expiryMinutes = _config.GetValue<int>("Jwt:ExpiryMinutes");
        var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenererRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"]!)),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Token invalide");

        return principal;
    }
}