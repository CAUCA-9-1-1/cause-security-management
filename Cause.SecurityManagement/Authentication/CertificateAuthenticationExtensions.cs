using Cause.SecurityManagement.Authentication.Certificate;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Cause.SecurityManagement.Authentication;

public static class CertificateAuthenticationExtensions
{
    public static IServiceCollection AddExternalCertificateAuthentication(
        this IServiceCollection services,
        string scheme = CustomAuthSchemes.CertificateAuthentication)
    {
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = scheme;
                options.DefaultChallengeScheme = scheme;
                options.DefaultScheme = scheme;
            })
            .AddScheme<CertificateAuthenticationOptions, CertificateAuthenticationHandler>(scheme, _ => { });

        return services;
    }

    /// <summary>
    /// Adds certificate authentication with a custom handler.
    /// </summary>
    /// <typeparam name="THandler">The type of the custom authentication handler.</typeparam>
    public static IServiceCollection AddCertificateAuthenticationWithCustomHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(this IServiceCollection services)
        where THandler : AuthenticationHandler<CertificateAuthenticationOptions>
    {
        services.AddAuthentication()
            .AddScheme<CertificateAuthenticationOptions, THandler>(CustomAuthSchemes.ConsoleCertificateAuthentication, _ => { });

        return services;
    }
}