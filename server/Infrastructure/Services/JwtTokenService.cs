using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Common.DTOs.Auth;
using Application.Features.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public AccessTokenResult GenerateAccessToken(TokenUserInfo user)
    {
        ValidateUserInfo(user);

        var jwtKey = GetRequiredConfig("Jwt:SecretKey");
        var issuer = GetRequiredConfig("Jwt:Issuer");
        var audience = GetRequiredConfig("Jwt:Audience");

        if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
        {
            throw new InvalidOperationException("JWT secret key must be at least 32 bytes.");
        }

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(GetTokenMinutes());

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

            new Claim(ClaimTypes.NameIdentifier, user.UserId),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName)
        };

        if (!string.IsNullOrWhiteSpace(user.Role))
        {
            claims.Add(new Claim(ClaimTypes.Role, user.Role));
        }

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        );

        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new AccessTokenResult
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresAtUtc = expiresAtUtc
        };
    }

    private static void ValidateUserInfo(TokenUserInfo user)
    {
        if (string.IsNullOrWhiteSpace(user.UserId))
        {
            throw new ArgumentException("UserId is required to generate token.");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new ArgumentException("Email is required to generate token.");
        }

        if (string.IsNullOrWhiteSpace(user.FullName))
        {
            throw new ArgumentException("FullName is required to generate token.");
        }
    }

    private string GetRequiredConfig(string key)
    {
        var value = _configuration[key];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing required configuration: {key}");
        }

        return value;
    }

    private int GetTokenMinutes()
    {
        var rawValue = _configuration["Jwt:AccessTokenMinutes"]
            ?? _configuration["Jwt:ExpiryMinutes"];

        if (int.TryParse(rawValue, out var minutes) && minutes > 0)
        {
            return minutes;
        }

        return 15;
    }
}