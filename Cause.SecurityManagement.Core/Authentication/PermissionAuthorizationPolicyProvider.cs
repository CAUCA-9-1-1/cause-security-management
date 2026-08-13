using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Cause.SecurityManagement.Core.Authentication;

/// <summary>
/// Creates permission policies on demand so consuming applications never enumerate their
/// permission tags in startup code. Any policy name that the default provider already
/// recognizes — including one that happens to carry the Permission: prefix — is returned
/// unchanged, so existing named policies keep working. Otherwise, the dynamic policy
/// inherits the application's AuthorizationOptions.FallbackPolicy in full, including its
/// requirements and authentication schemes, and adds the permission requirement on top:
/// a decorated endpoint is therefore always at least as strict as an undecorated one.
/// Registering this provider requires the application's authentication scheme list to
/// live on AuthorizationOptions.FallbackPolicy — schemes configured only on DefaultPolicy
/// are not inherited.
/// </summary>
public class PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    : IAuthorizationPolicyProvider
{
    private readonly AuthorizationOptions authorizationOptions = options.Value;
    private readonly DefaultAuthorizationPolicyProvider fallbackProvider = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => fallbackProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy> GetFallbackPolicyAsync() => fallbackProvider.GetFallbackPolicyAsync();

    /// <summary>
    /// The built policy is a pure function of the policy name plus AuthorizationOptions,
    /// which is a singleton snapshot fixed after startup, so caching is always safe.
    /// </summary>
    public bool AllowsCachingPolicies => true;

    public async Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
    {
        ArgumentNullException.ThrowIfNull(policyName);

        var explicitPolicy = await fallbackProvider.GetPolicyAsync(policyName);
        if (explicitPolicy is not null)
            return explicitPolicy;

        if (!PermissionPolicy.TryParse(policyName, out var tag, out var allowAdministrator))
            return null;

        return BuildPolicy(tag, allowAdministrator);
    }

    private AuthorizationPolicy BuildPolicy(string tag, bool allowAdministrator)
    {
        var builder = new AuthorizationPolicyBuilder().RequireAuthenticatedUser();

        var fallbackPolicy = authorizationOptions.FallbackPolicy;
        if (fallbackPolicy is not null)
            builder.Combine(fallbackPolicy);

        builder.AddRequirements(new PermissionRequirement(tag, allowAdministrator));

        return builder.Build();
    }
}
