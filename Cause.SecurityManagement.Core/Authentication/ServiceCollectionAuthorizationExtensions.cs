using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Cause.SecurityManagement.Core.Authentication;

/// <summary>
/// Extension methods for adding authorization policies to the service collection.
/// </summary>
public static class ServiceCollectionAuthorizationExtensions
{
    /// <summary>
    /// Add authorization policies for external systems.
    /// Will require authenticated users to have the 'ExternalSystem' role by default unless a different policy is specified on the endpoint.
    /// Also registers the single-401-rejection logger (see AddAuthorizationRejectionLogging)
    /// unless <paramref name="logAuthorizationRejections"/> is false.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="logAuthorizationRejections">
    /// When true (the default), also calls AddAuthorizationRejectionLogging(). Pass false to opt out.
    /// </param>
    /// <returns></returns>
    public static IServiceCollection AddAuthorizationForExternalSystem(
        this IServiceCollection services,
        bool logAuthorizationRejections = true)
    {
        services.AddAuthorizationCore(options =>
        {
            options
                .AddExternalSystemPolicy()
                .FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireRole(SecurityRoles.ExternalSystem)
                .Build();
        });

        if (logAuthorizationRejections)
            services.AddAuthorizationRejectionLogging();

        return services;
    }


    /// <summary>
    /// Add authorization policies for regular users with policies for user recovery, user creation, user password setup, and metrics.
    /// Also includes policies for console certificates.
    /// Will require user to have either the 'RegularUser', 'Console', or 'Administrator' role by default unless a different policy is specified on the endpoint.
    /// Also registers the single-401-rejection logger (see AddAuthorizationRejectionLogging)
    /// unless <paramref name="logAuthorizationRejections"/> is false.
    /// </summary>
    public static IServiceCollection AddAuthorizationForRegularUserKeycloakAndApiCertificate(
        this IServiceCollection services,
        bool logAuthorizationRejections = true)
    {
        services
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

        if (logAuthorizationRejections)
            services.AddAuthorizationRejectionLogging();

        return services;
    }

    /// <summary>
    /// Add authorization policies for Keycloak and regular users with a policy for metrics.
    /// Will require authenticated users to have the 'RegularUser' role by default unless a different policy is specified on the endpoint.
    /// This does not include policies for user recovery, user creation, or user password setup.
    /// Also registers the single-401-rejection logger (see AddAuthorizationRejectionLogging)
    /// unless <paramref name="logAuthorizationRejections"/> is false.
    /// </summary>
    public static IServiceCollection AddAuthorizationForKeycloakAndRegularUserSchemes(
        this IServiceCollection services,
        bool logAuthorizationRejections = true)
    {
        services.AddAuthorizationCore(options =>
        {
            options.AddMetricsPolicy();
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(CustomAuthSchemes.KeycloakAuthentication, CustomAuthSchemes.RegularUserAuthentication)
                .RequireRole(SecurityRoles.Administrator)
                .Build();
        });

        if (logAuthorizationRejections)
            services.AddAuthorizationRejectionLogging();

        return services;
    }

    /// <summary>
    /// Add authorization policies for regular users AND external systems with policies for user recovery, user creation, user password setup, and metrics.
    /// Will require authenticated users to have the 'RegularUser' role by default unless a different policy is specified on the endpoint.
    /// Also registers the single-401-rejection logger (see AddAuthorizationRejectionLogging)
    /// unless <paramref name="logAuthorizationRejections"/> is false.
    /// </summary>
    public static IServiceCollection AddAuthorizationForRegularUserAndExternalSystem(
        this IServiceCollection services,
        bool logAuthorizationRejections = true)
    {
        services.AddAuthorizationCore(options =>
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

        if (logAuthorizationRejections)
            services.AddAuthorizationRejectionLogging();

        return services;
    }

    /// <summary>
    /// Add authorization policies for regular users with policies for user recovery, user creation, user password setup, and metrics.
    /// Will require authenticated users to have the 'RegularUser' role by default unless a different policy is specified on the endpoint.
    /// Also registers the single-401-rejection logger (see AddAuthorizationRejectionLogging)
    /// unless <paramref name="logAuthorizationRejections"/> is false.
    /// </summary>
    public static IServiceCollection AddAuthorizationForRegularUser(
        this IServiceCollection services,
        bool logAuthorizationRejections = true)
    {
        services.AddAuthorizationCore(options =>
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

        if (logAuthorizationRejections)
            services.AddAuthorizationRejectionLogging();

        return services;
    }

    /// <summary>
    /// Adds the permission gate used by [AdministratorOrUserWithPermission] and
    /// [UserWithPermission]. Opt-in, and composes with the AddAuthorizationFor* extensions
    /// rather than replacing any of them. Call order relative to those extensions does not
    /// matter.
    /// Also registers the authorization services required by UseAuthorization() — the
    /// AddAuthorizationFor* extensions call AddAuthorizationCore alone, which is not enough for
    /// UseAuthorization()'s VerifyServicesRegistered check, so a minimal-API application relying
    /// solely on this library would otherwise crash at startup.
    /// Not useful with AddAuthorizationForKeycloakAndRegularUserSchemes or
    /// AddAuthorizationForExternalSystem — see the remarks.
    /// When validateTagsAtStartup is true, also registers a hosted service that warns at
    /// startup when a permission attribute names a tag absent from the permission catalog,
    /// via IPermissionCatalogService. Defaults to false so existing call sites keep their
    /// current behavior unchanged.
    /// </summary>
    /// <remarks>
    /// The dynamic policy inherits the application's AuthorizationOptions.FallbackPolicy, so a
    /// decorated endpoint is always at least as strict as an undecorated one. Two registrations
    /// therefore leave the gate with nothing useful to do:
    /// AddAuthorizationForKeycloakAndRegularUserSchemes admits only Administrator, whom the
    /// handler passes unconditionally, so [AdministratorOrUserWithPermission] is a no-op and
    /// [UserWithPermission] denies everyone; AddAuthorizationForExternalSystem admits only
    /// ExternalSystem, which the handler always denies, so both attributes deny everyone.
    /// </remarks>
    public static IServiceCollection AddPermissionBasedAuthorization(
        this IServiceCollection services,
        bool validateTagsAtStartup = false)
    {
        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.Replace(ServiceDescriptor.Singleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, PermissionAuthorizationHandler>());
        services.TryAddScoped<ScopedPermissionCache>();

        if (validateTagsAtStartup)
            services.AddHostedService<PermissionTagValidationHostedService>();

        return services;
    }

    /// <summary>
    /// Logs exactly one Information line per request whenever the authorization middleware ends
    /// in a 401 challenge, naming the authentication scheme(s) it consulted and, for each, the
    /// reason it did not authenticate (no credentials presented, or the concrete authentication
    /// failure). Every AddAuthorizationFor* extension in this library already calls this by
    /// default (pass logAuthorizationRejections: false to one of them to opt out), so most
    /// consumers never need to call it directly - it remains public for applications that build
    /// their authorization policies without those extensions. It replaces the application's
    /// IAuthorizationMiddlewareResultHandler outright: only one such handler runs per request. It
    /// stays safe to add because it delegates every 401/403/success decision to the framework's
    /// own AuthorizationMiddlewareResultHandler unchanged - it only adds the one log line before
    /// that delegation.
    /// </summary>
    /// <remarks>
    /// Call this anywhere during service configuration. It replaces the registration (via
    /// Replace, not TryAdd), so call order relative to any AddAuthorizationFor* or
    /// AddAuthorization call does not matter - the framework registers its own default with
    /// a TryAdd (registered Transient, via AddAuthorizationPolicyEvaluator), so this always wins regardless of order. Calling it more than once (for
    /// example once from an AddAuthorizationFor* extension and once directly) is harmless: Replace
    /// leaves exactly one registered descriptor.
    /// If your application already registers its own IAuthorizationMiddlewareResultHandler, do
    /// not call this (directly, or indirectly via an AddAuthorizationFor* extension's default
    /// parameter): it replaces that registration and delegates to the framework's built-in
    /// handler, not to yours, so your own handler would silently stop running. Compose the two
    /// manually instead. This is not only a documentation warning: if a pre-existing custom
    /// handler is detected, a hosted service logs a Warning at startup naming the displaced
    /// type, so the displacement is visible even if this paragraph goes unread.
    /// The composed reason includes JWT authentication failure messages verbatim. If a consumer
    /// sets IdentityModelEventSource.ShowPII = true (a debugging aid), those messages stop
    /// redacting token and key material, and that material would reach this log line.
    /// This does not silence the framework's own per-scheme authentication diagnostics (for
    /// example "{scheme} was not authenticated..." or "AuthenticationScheme: {scheme} was
    /// challenged."), which AuthenticationHandler logs under that handler's own type as category
    /// (e.g. "Cause.SecurityManagement.Core.Authentication.Certificate.CertificateAuthenticationHandler").
    /// A consumer who wants only this single line can raise those handler categories to Warning
    /// in appsettings logging configuration and keep this logger's category
    /// ("Cause.SecurityManagement.Core.Authentication.AuthorizationRejectionLogger") at
    /// Information.
    /// An error endpoint reached through UseStatusCodePagesWithReExecute must be
    /// [AllowAnonymous], or the re-executed request is itself rejected and produces a second
    /// rejection line for what the client experiences as a single 401 (one line for the original
    /// path, one for the re-executed error path).
    /// When the default authentication scheme is a policy scheme that forwards to another scheme
    /// (as with AddDualExternalSystemAuthentication), the logged scheme name is the policy
    /// scheme's name, not the forwarded scheme that actually failed - the concrete reason in the
    /// same line still distinguishes a missing certificate from an invalid token.
    /// </remarks>
    public static IServiceCollection AddAuthorizationRejectionLogging(this IServiceCollection services)
    {
        WarnIfDisplacingCustomResultHandler(services);
        services.Replace(ServiceDescriptor.Singleton<IAuthorizationMiddlewareResultHandler, AuthorizationRejectionLogger>());
        return services;
    }

    /// <summary>
    /// Detects an application-registered IAuthorizationMiddlewareResultHandler before it gets
    /// replaced, and - if one is found and it is neither the framework default nor our own
    /// logger - registers a hosted service that warns about the displacement once the
    /// application actually starts. No ILogger is reachable yet at this point (the service
    /// provider has not been built), so the warning cannot be emitted immediately; a hosted
    /// service is this repository's existing pattern for that (see
    /// PermissionTagValidationHostedService).
    /// </summary>
    private static void WarnIfDisplacingCustomResultHandler(IServiceCollection services)
    {
        var existing = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(IAuthorizationMiddlewareResultHandler));
        if (existing is null)
            return;

        var displacedType = existing.ImplementationType ?? existing.ImplementationInstance?.GetType();
        if (displacedType == typeof(AuthorizationMiddlewareResultHandler) || displacedType == typeof(AuthorizationRejectionLogger))
            return;

        var displacedTypeName = displacedType?.FullName
            ?? "a custom IAuthorizationMiddlewareResultHandler registered via a factory delegate";

        services.Replace(ServiceDescriptor.Singleton(new DisplacedAuthorizationResultHandlerWarning(displacedTypeName)));
        services.AddHostedService<AuthorizationResultHandlerDisplacementWarningService>();
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