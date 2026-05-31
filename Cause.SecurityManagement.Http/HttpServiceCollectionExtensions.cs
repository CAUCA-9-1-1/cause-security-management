using Cause.SecurityManagement.Authentication.Certificate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cause.SecurityManagement;

/// <summary>
/// Extension methods for registering Cause.SecurityManagement HTTP services (controllers, authorization policies).
/// </summary>
public static class HttpServiceCollectionExtensions
{
    /// <summary>
    /// Registers the three default authorization policies (defaultpolicy, apipolicy, apicertificatepolicy)
    /// used by AddAuthorizeFiltersControllerConvention.
    /// ICertificateValidator is already registered by InjectSecurityServices() in the core package.
    /// </summary>
    public static IServiceCollection AddBasicPoliciesForCertificateLogon(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("defaultpolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(SecurityRoles.User);
            });
            options.AddPolicy("apipolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(SecurityRoles.ExternalSystem);
            });
            options.AddPolicy("apicertificatepolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(SecurityRoles.ExternalSystem);
                policy.AddAuthenticationSchemes(CertificateAuthenticationOptions.Name, SecurityManagementOptions.AuthenticationScheme);
            });
        });
        return services;
    }
}
