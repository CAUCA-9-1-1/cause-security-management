using Cause.SecurityManagement.Models.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Cause.SecurityManagement.Authentication;

public static class TokenAuthenticationExtensions
{
    public static IServiceCollection AddTokenAuthentication(this IServiceCollection services, SecurityConfiguration configuration, KeycloakConfiguration keycloakConfiguration = null)
    {
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        services
            .AddScoped<IClaimsTransformation, MultiJwtClaimsTransformer>()
            .AddAuthenticationWithScheme()
            .AddRegularUserJwtBearer(configuration)
            .AddKeycloakAuthenticationBuilderWhenNeeded(keycloakConfiguration)
            .AddCustomPolicyScheme(keycloakConfiguration);
        return services.AddRedirectionCustomizationOnUnauthorized();
    }

    public static IServiceCollection AddSimpleTokenAuthentication(this IServiceCollection services, SecurityConfiguration configuration)
    {
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        services
            .AddSimpleAuthentication()
            .AddSimpleAuthentication(configuration);
        return services.AddRedirectionCustomizationOnUnauthorized();
    }

    internal static AuthenticationBuilder AddCustomPolicyScheme(this AuthenticationBuilder builder, KeycloakConfiguration configuration)
    {
        return builder.AddPolicyScheme(CustomAuthSchemes.RegularUserOrKeycloakScheme, CustomAuthSchemes.RegularUserOrKeycloakScheme, options =>
        {
            options.ForwardDefaultSelector = context => GetSchemeToUse(configuration, context);
        });
    }

    private static string GetSchemeToUse(KeycloakConfiguration configuration, HttpContext context)
    {
        string authorization = context.Request.Headers[HeaderNames.Authorization];
        if (configuration != null && !string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
        {
            var token = authorization.Substring("Bearer ".Length).Trim();
            var jwtHandler = new JwtSecurityTokenHandler();
            return jwtHandler.ReadJwtToken(token).Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Iss)?.Value == configuration.ValidIssuer ? CustomAuthSchemes.KeycloakAuthentication : CustomAuthSchemes.RegularUserAuthentication;
        }
        return CustomAuthSchemes.RegularUserAuthentication;
    }
}