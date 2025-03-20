using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cause.SecurityManagement.Authentication;

public static class ServiceCollectionAuthorizationExtensions
{
    public static IServiceCollection AddAuthorizationForKeycloakAndRegularUserSchemes(this IServiceCollection services)
    {
        return services.AddAuthorizationCore(options =>
        {
            options.AddPolicy(SecurityPolicy.Metrics,
                policy => policy.RequireAssertion(_ => true));
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(CustomAuthSchemes.KeycloakAuthentication, CustomAuthSchemes.RegularUserAuthentication)
                .RequireRole(SecurityRoles.Administrator)
                .Build();
        });
    }

    public static IServiceCollection AddAuthorizationForRegularUser(this IServiceCollection services)
    {
        return services.AddAuthorizationCore(options =>
        {
            options.AddPolicy(SecurityPolicy.UserRecovery, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(SecurityRoles.UserAndUserRecovery));
            options.AddPolicy(SecurityPolicy.UserCreation, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(SecurityRoles.UserAndUserCreation));
            options.AddPolicy(SecurityPolicy.Metrics, policy => policy
                .RequireAssertion(_ => true));

            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireRole(SecurityRoles.User).Build();
        });
    }
}