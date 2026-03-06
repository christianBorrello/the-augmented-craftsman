using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TacBlog.Application.Ports.Driven;

namespace TacBlog.Infrastructure.Identity;

public sealed record JwtSettings(string Secret, string Issuer, int ExpiryInMinutes = 60);

public sealed class JwtTokenGenerator(JwtSettings settings) : ITokenGenerator
{
    public string Generate(string email)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, email),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Issuer,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(settings.ExpiryInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
