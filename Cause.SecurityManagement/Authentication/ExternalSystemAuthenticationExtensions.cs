using Cause.SecurityManagement.Core;
using Cause.SecurityManagement.Core.Authentication;
using Cause.SecurityManagement.Core.Authentication.Certificate;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Authentication;

/// <summary>
/// Extension methods for adding dual (certificate or token) authentication for external systems.
/// </summary>
public static class ExternalSystemAuthenticationExtensions
{
    /// <summary>
    /// Adds an authentication scheme that authenticates external systems either by client certificate or by bearer token.
    /// The scheme to use is selected based on the presence of a Bearer token in the Authorization header.
    /// </summary>
    public static IServiceCollection AddDualExternalSystemAuthentication(this IServiceCollection services, SecurityConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        services.AddScoped<IClaimsTransformation, ExternalSystemSidClaimsTransformer>();

        services
            .AddAuthenticationWithScheme(CustomAuthSchemes.DualExternalSystemScheme)
            .AddScheme<CertificateAuthenticationOptions, CertificateAuthenticationHandler>(CustomAuthSchemes.CertificateAuthentication, _ => { })
            .AddJwtBearer(CustomAuthSchemes.ExternalSystemTokenAuthentication, options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = RegularUserJwtAuthenticationBuilder.GetAuthenticationParameters(
                    configuration.SecretKey, configuration.Issuer, configuration.PackageName);
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = RegularUserJwtAuthenticationBuilder.GetCustomOnAuthenticationFailedResult,
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity is ClaimsIdentity identity
                            && context.Principal.HasClaim(claim => claim.Type == ClaimTypes.Role && claim.Value == SecurityRoles.ExternalSystem)
                            && !identity.HasClaim(claim => claim.Type == ExternalSystemClaims.AuthenticationType))
                        {
                            identity.AddClaim(new Claim(ExternalSystemClaims.AuthenticationType, ExternalSystemAuthenticationType.Token.ToString()));
                        }
                        return Task.CompletedTask;
                    }
                };
            })
            .AddPolicyScheme(CustomAuthSchemes.DualExternalSystemScheme, CustomAuthSchemes.DualExternalSystemScheme, options =>
            {
                options.ForwardDefaultSelector = context => HasBearerToken(context)
                    ? CustomAuthSchemes.ExternalSystemTokenAuthentication
                    : CustomAuthSchemes.CertificateAuthentication;
            });

        return services;
    }

    private static bool HasBearerToken(HttpContext context)
    {
        string? authorization = context.Request.Headers[HeaderNames.Authorization].FirstOrDefault();
        return !string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
    }
}
