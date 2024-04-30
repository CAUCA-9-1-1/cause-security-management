using Cause.SecurityManagement.Models.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Cause.SecurityManagement.Services;

public class TokenGenerator(IOptions<SecurityConfiguration> configuration) : ITokenGenerator
{
    public readonly int DefaultRefreshTokenLifetimeInMinutes = 9 * 60;
    public readonly int DefaultAccessTokenLifetimeInMinutes = 60;
    public readonly int DefaultTemporaryAccessTokenLifetimeInMinutes = 5;

    private readonly SecurityConfiguration configuration = configuration.Value;

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public string GenerateAccessToken(string entityId, string entityName, string role, params (string type, string value)[] additionalClaims)
    {
        var lifeTimeInMinute = SecurityRoles.IsTemporaryRole(role) ? GetTemporaryAccessTokenLifeTimeInMinute() : GetAccessTokenLifeTimeInMinute();

        var claims = new List<Claim> 
        {
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.UniqueName, entityName),
            new(JwtRegisteredClaimNames.Sid, entityId),
        };

        if (additionalClaims != null)
        {
            claims.AddRange(additionalClaims.Select(claim => new Claim(claim.type, claim.value)));
        }

        return GenerateAccessToken(claims.ToArray(), lifeTimeInMinute);
    }

    public DateTime GenerateRefreshTokenExpirationDate()
    {
        var lifeTimeInMinute = GetRefreshTokenLifeTimeInMinute();
        if (configuration.RefreshTokenCanExpire)
        {
            return DateTime.Now.AddMinutes(lifeTimeInMinute);
        }
        return DateTime.MaxValue;
    }

    protected string GenerateAccessToken(Claim[] claims, int lifeTimeInMinute)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(configuration.Issuer,
            configuration.PackageName,
            claims,
            notBefore: DateTime.Now,
            expires: DateTime.Now.AddMinutes(lifeTimeInMinute),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public int GetRefreshTokenLifeTimeInMinute()
    {
        return configuration.RefreshTokenLifeTimeInMinutes ?? DefaultRefreshTokenLifetimeInMinutes;
    }

    public int GetTemporaryAccessTokenLifeTimeInMinute()
    {
        return configuration.TemporaryAccessTokenLifeTimeInMinutes ?? DefaultTemporaryAccessTokenLifetimeInMinutes;
    }

    public int GetAccessTokenLifeTimeInMinute()
    {
        return configuration.AccessTokenLifeTimeInMinutes ?? DefaultAccessTokenLifetimeInMinutes;
    }
}