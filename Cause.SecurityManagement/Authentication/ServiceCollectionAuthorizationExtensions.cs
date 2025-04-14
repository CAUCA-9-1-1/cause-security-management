using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cause.SecurityManagement.Authentication;

public static class ServiceCollectionAuthorizationExtensions
{
    public static IServiceCollection AddAuthorizationForKeycloakAndRegularUserSchemes(this IServiceCollection services)
    {
        return services.AddAuthorizationCore(options =>
        {
            options.AddMetricsPolicy();
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(CustomAuthSchemes.KeycloakAuthentication, CustomAuthSchemes.RegularUserAuthentication)
                .RequireRole(SecurityRoles.Administrator)
                .Build();
        });
    }

    public static IServiceCollection AddAuthorizationForRegularUserAndExternalSystem(this IServiceCollection services)
    {
        return services.AddAuthorizationCore(options =>
        {
            options
                .AddUserRecoveryPolicy()
                .AddUserCreationPolicy()
                .AddUserPasswordSetupPolicy()
                .AddMetricsPolicy()
                .AddExternalSystemPolicy()
                .FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireRole(SecurityRoles.User).Build();
        });
    }

    public static IServiceCollection AddAuthorizationForRegularUser(this IServiceCollection services)
    {
        return services.AddAuthorizationCore(options =>
        {
            options
                .AddUserRecoveryPolicy()
                .AddUserCreationPolicy()
                .AddUserPasswordSetupPolicy()
                .AddMetricsPolicy()
                .FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireRole(SecurityRoles.User).Build();
        });
    }

    private static AuthorizationOptions AddExternalSystemPolicy(this AuthorizationOptions options)
    {
        options.AddPolicy(SecurityPolicy.ExternalSystem, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(SecurityRoles.ExternalSystem));
        return options;
    }

    private static AuthorizationOptions AddMetricsPolicy(this AuthorizationOptions options)
    {
        options.AddPolicy(SecurityPolicy.Metrics, policy => policy
            .RequireAssertion(_ => true));
        return options;
    }

    private static AuthorizationOptions AddUserCreationPolicy(this AuthorizationOptions options)
    {
        options.AddPolicy(SecurityPolicy.UserCreation, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(SecurityRoles.UserAndUserCreation));
        return options;
    }

    private static AuthorizationOptions AddUserRecoveryPolicy(this AuthorizationOptions options)
    {
        options.AddPolicy(SecurityPolicy.UserRecovery, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(SecurityRoles.UserAndUserRecovery));
        return options;
    }

    private static AuthorizationOptions AddUserPasswordSetupPolicy(this AuthorizationOptions options)
    {
        options.AddPolicy(SecurityPolicy.UserPasswordSetup, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(SecurityRoles.UserPasswordSetup));
        return options;
    }
}