using Cause.SecurityManagement.Authentication.Certificate;
using Microsoft.Extensions.DependencyInjection;

namespace Cause.SecurityManagement.Authentication;

public static class CertificateAuthenticationExtensions
{
    public static IServiceCollection AddCertificateAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(CertificateAuthenticationOptions.Name)
            .AddScheme<CertificateAuthenticationOptions, CertificateAuthenticationHandler>(CertificateAuthenticationOptions.Name, options => { });

        return services;
    }
}