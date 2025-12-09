using Cause.SecurityManagement.Authentication.Certificate;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Cause.SecurityManagement.Authentication;

public static class CertificateAuthenticationExtensions
{
    /// <summary>
    /// Add certificate authentication for external systems.
    /// </summary>
    public static IServiceCollection AddExternalCertificateAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication()
            .AddScheme<CertificateAuthenticationOptions, CertificateAuthenticationHandler>(CustomAuthSchemes.CertificateAuthentication, _ => { });

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