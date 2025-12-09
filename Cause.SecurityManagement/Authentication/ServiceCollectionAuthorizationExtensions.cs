using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cause.SecurityManagement.Authentication;

/// <summary>
/// Extension methods for adding authorization policies to the service collection.
/// </summary>
public static class ServiceCollectionAuthorizationExtensions
{
    /// <summary>
    /// Add authorization policies for regular users with policies for user recovery, user creation, user password setup, and metrics.
    /// Also includes policies for console certificates.
    /// Will require user to have either the 'RegularUser', 'Console', or 'Administrator' role by default unless a different policy is specified on the endpoint.
    /// </summary>
    public static IServiceCollection AddAuthorizationForRegularUserKeycloakAndApiCertificate(this IServiceCollection services)
    {
        return services
            .AddAuthorizationCore(options =>
            {
                options
                    .AddConsoleCertificatePoliciy()
                    .AddUserRecoveryPolicy()
                    .AddUserPasswordSetupPolicy()
                    .AddMetricsPolicy()
                    .FallbackPolicy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .AddAuthenticationSchemes(CustomAuthSchemes.KeycloakAuthentication, CustomAuthSchemes.RegularUserAuthentication, CustomAuthSchemes.ConsoleCertificateAuthentication)
                        .RequireRole(SecurityRoles.User, SecurityRoles.ApiCertificate, SecurityRoles.Administrator)
                        .Build();
            });
    }

    /// <summary>
    /// Add authorization policies for Keycloak and regular users with a policy for metrics.
    /// Will require authenticated users to have the 'RegularUser' role by default unless a different policy is specified on the endpoint.
    /// This does not include policies for user recovery, user creation, or user password setup.
    /// </summary>
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

    /// <summary>
    /// Add authorization policies for regular users AND external systems with policies for user recovery, user creation, user password setup, and metrics.
    /// Will require authenticated users to have the 'RegularUser' role by default unless a different policy is specified on the endpoint.
    /// </summary>
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

    /// <summary>
    /// Add authorization policies for regular users with policies for user recovery, user creation, user password setup, and metrics.
    /// Will require authenticated users to have the 'RegularUser' role by default unless a different policy is specified on the endpoint.
    /// </summary>
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
                .RequireRole(SecurityRoles.User)
                .Build();
        });
    }

    /// <summary>
    /// Add authorization policy for console certificate authentication.
    /// </summary>
    private static AuthorizationOptions AddConsoleCertificatePoliciy(this AuthorizationOptions options)
    {

        options.AddPolicy(SecurityPolicy.ApiCertificate, policy => policy
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(CustomAuthSchemes.ConsoleCertificateAuthentication)
            .RequireRole(SecurityRoles.ApiCertificate));
        return options;
    }

    /// <summary>
    /// Add authorization policy for external system authentication. 
    /// </summary>
    private static AuthorizationOptions AddExternalSystemPolicy(this AuthorizationOptions options)
    {
        options.AddPolicy(SecurityPolicy.ExternalSystem, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(SecurityRoles.ExternalSystem));
        return options;
    }

    /// <summary>
    /// Add authorization policy for metrics access (promotheus).     
    /// </summary>
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