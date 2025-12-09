using Cause.SecurityManagement.Models.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Cause.SecurityManagement.Authentication.Certificate;

namespace Cause.SecurityManagement.Authentication;

/// <summary>
/// Extension methods for adding token authentication to the service collection.
/// </summary>
public static class TokenAuthenticationExtensions
{
    /// <summary>
    /// Add triple token authentication (keycloak, internal user, console) to the service collection.
    /// This method configures JWT bearer authentication for regular users, console certificates, and Keycloak using the provided security configuration and optional Keycloak configuration.
    /// It also allows specifying a custom authentication handler for certificate authentication.
    /// </summary>
    public static IServiceCollection AddTokenAuthenticationWithCertificates<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(this IServiceCollection services, SecurityConfiguration configuration, KeycloakConfiguration keycloakConfiguration = null)
        where THandler : AuthenticationHandler<CertificateAuthenticationOptions>
    {
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        services
            .AddScoped<IClaimsTransformation, MultiJwtClaimsTransformer>()
            .AddAuthenticationWithScheme(CustomAuthSchemes.RegularUserKeycloakOrConsoleScheme)
            .AddRegularUserJwtBearer(configuration)
            .AddKeycloakAuthenticationBuilderWhenNeeded(keycloakConfiguration)
            .AddCustomPolicyScheme(keycloakConfiguration, CustomAuthSchemes.RegularUserKeycloakOrConsoleScheme);
        services.AddCertificateAuthenticationWithCustomHandler<THandler>();
        return services.AddRedirectionCustomizationOnUnauthorized();
    }

    /// <summary>
    /// Add dual token authentication (keycloak and internal user) to the service collection.
    /// This method configures JWT bearer authentication for both regular users and Keycloak using the provided security configuration and optional Keycloak configuration.
    /// </summary>
    public static IServiceCollection AddTokenAuthentication(this IServiceCollection services, SecurityConfiguration configuration, KeycloakConfiguration keycloakConfiguration = null)
    {
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        services
            .AddScoped<IClaimsTransformation, MultiJwtClaimsTransformer>()
            .AddAuthenticationWithScheme(CustomAuthSchemes.RegularUserOrKeycloakScheme)
            .AddRegularUserJwtBearer(configuration)
            .AddKeycloakAuthenticationBuilderWhenNeeded(keycloakConfiguration)
            .AddCustomPolicyScheme(keycloakConfiguration, CustomAuthSchemes.RegularUserOrKeycloakScheme);
        return services.AddRedirectionCustomizationOnUnauthorized();
    }

    /// <summary>
    /// Adds simple token authentication to the service collection.
    /// This method configures JWT bearer authentication using the provided security configuration. 
    /// </summary>
    public static IServiceCollection AddSimpleTokenAuthentication(this IServiceCollection services, SecurityConfiguration configuration)
    {
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        services
            .AddSimpleAuthentication()
            .AddSimpleAuthentication(configuration);
        return services.AddRedirectionCustomizationOnUnauthorized();
    }

    /// <summary>
    /// This method add a way to select the right authentication scheme based on the incoming request/token.
    /// </summary>
    internal static AuthenticationBuilder AddCustomPolicyScheme(this AuthenticationBuilder builder, KeycloakConfiguration configuration, string multiSchemeName)
    {
        return builder.AddPolicyScheme(multiSchemeName, multiSchemeName, options =>
        {
            options.ForwardDefaultSelector = context => GetSchemeToUse(configuration, context);
        });
    }

    private static string GetSchemeToUse(KeycloakConfiguration configuration, HttpContext context)
    {
        string authorization = context.Request.Headers[HeaderNames.Authorization];
        if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith("Bearer "))
            return CustomAuthSchemes.RegularUserAuthentication;
        var token = authorization.Substring("Bearer ".Length).Trim();
        var jwtHandler = new JwtSecurityTokenHandler();
        if (IsKeycloakToken(configuration, jwtHandler, token))
            return CustomAuthSchemes.KeycloakAuthentication;
        if (IsRegularUser(jwtHandler, token))
            return CustomAuthSchemes.RegularUserAuthentication;
        if (IsConsole(jwtHandler, token))
            return CustomAuthSchemes.ConsoleCertificateAuthentication;
        return CustomAuthSchemes.RegularUserAuthentication;
    }

    private static bool IsKeycloakToken(KeycloakConfiguration configuration, JwtSecurityTokenHandler jwtHandler, string token)
        => configuration != null && jwtHandler.ReadJwtToken(token).Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Iss)?.Value == configuration.ValidIssuer;

    private static bool IsRegularUser(JwtSecurityTokenHandler jwtHandler, string token)
        => jwtHandler.ReadJwtToken(token).Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == SecurityRoles.User);

    private static bool IsConsole(JwtSecurityTokenHandler jwtHandler, string token)
        => jwtHandler.ReadJwtToken(token).Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == SecurityRoles.ApiCertificate);
}