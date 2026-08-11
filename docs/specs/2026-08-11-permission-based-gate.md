# Spec: Permission-Based Authorization Gate

* Issue: #115
* Date: 2026-08-11
* Status: approved for implementation
* Related ADR: [2026-08-11-permission-based-authorization-gate.md](../adr/records/2026-08-11-permission-based-authorization-gate.md)

## Goal

Provide a granular, per-endpoint permission gate usable across all API patterns
supported by the library.

```csharp
[RequirePermission("CanEditBuilding")]
public async Task<IActionResult> EditBuilding(...) { }
```

## Authorization Rule

Only `RegularUser` principals are gated by permissions. Every other principal
type keeps the behavior it has today.

| Principal | Result | Database access |
|---|---|---|
| `Administrator` | pass | none |
| `RegularUser` | pass only if the merged permission set allows the tag | one read |
| `ExternalSystem` | fail (403) | none |
| `Console` (`ApiCertificate`) | fail (403) | none |
| Temporary roles (`UserCreation`, `UserRecovery`, `UserPasswordSetup`, `UserLoginWithMultiFactor`) | fail (403) | none |
| No recognized role | fail (403) | none |

The handler fails closed: it succeeds only on an explicit positive match and
otherwise leaves the requirement unsatisfied.

### Permission Merge Semantics

The gate reuses the existing merge semantics in
`Cause.SecurityManagement.Core/PermissionMergeTool.cs`. `Access` is computed as
`group.All(p => p.Access)`, so **deny wins**: a user-level allow does not
override a group-level deny for the same tag. This spec does not change that
behavior; it documents the dependency so the handler's outcome is predictable.

## Critical Constraint: The Default Policy Stands Down

`Cause.SecurityManagement/UseDefaultAuthorizationWhenNotSpecifiedFilter.cs`
returns without evaluating its policy whenever another authorization filter is
present on the endpoint:

```csharp
if (context.Filters.Any(item => item is IAsyncAuthorizationFilter && item != this))
    return Task.FromResult(0);
```

`Cause.SecurityManagement/AddAuthorizeFiltersControllerConvention.cs` likewise
skips the default filter when the controller type carries any
`AuthorizeAttribute`.

Because `RequirePermissionAttribute` derives from `AuthorizeAttribute`, decorating
an endpoint with it **suppresses the application's configured fallback policy on
that endpoint**. The `Permission:` policy becomes the only gate.

Two requirements follow, and both are load-bearing for security rather than
stylistic:

1. The dynamic policy must include `RequireAuthenticatedUser()` itself.
2. The handler must verify the role positively, because no other filter is
   verifying it.

## Policy Composition

The dynamic policy is built as:

* `RequireAuthenticatedUser()`
* the authentication scheme list read from
  `AuthorizationOptions.FallbackPolicy?.AuthenticationSchemes`, obtained by
  injecting `IOptions<AuthorizationOptions>` into the provider. Empty when the
  application configured no fallback policy or named no schemes.
* `PermissionRequirement(tag)`

The policy deliberately does **not** call `RequireRole`. Role differentiation
lives in the handler so that `[RequirePermission]` behaves identically in
`RegularUser`-default and `Administrator`-default applications.

### Why The Scheme List Is Copied

`AddAuthorizationForRegularUserKeycloakAndApiCertificate` sets an explicit
three-scheme list on its fallback policy
(`Cause.SecurityManagement.Core/Authentication/ServiceCollectionAuthorizationExtensions.cs`).
A dynamic policy that omits the scheme list is evaluated against the default
scheme only, which risks a 401 for principals authenticated under one of the
other schemes even when they hold the permission.

This is a hypothesis about ASP.NET Core policy evaluation, not a verified fact.
It must be confirmed by the integration test listed below rather than assumed —
if the test shows the scheme list is unnecessary, drop the copying and simplify.

## Caching

**Decision: per-request memoization only. Cross-request caching is out of scope.**

`UserPermissionService.HasPermission` issues two database reads per call (user
permissions and group permissions, each with an `Include` on `Permission`). A
single request can trigger several checks: multiple requirements on one endpoint,
or the handler followed by a business-layer call such as the one in
`UserGroupPermissionService`.

A scoped cache keyed by user id collapses those to one read. It carries no
staleness risk, because a user's permission set cannot meaningfully change within
a single request, and it needs no invalidation logic.

### Why Cross-Request TTL Caching Is Deferred

Recorded so this is not re-litigated without new information:

1. **Invalidation fans out.** The merged set derives from both `UserPermission`
   and `GroupPermission` rows. Revoking a group permission, or removing one user
   from a group, must evict every affected user rather than a single key. That
   requires eviction hooks in the group-management write paths, well outside the
   blast radius of this issue.
2. **In-memory eviction does not survive horizontal scaling.** With several
   instances, an eviction on one leaves the others serving stale grants until
   the TTL expires. Correctness would rest on the TTL alone, not on invalidation,
   and that limitation must be stated plainly rather than implied away.
3. **This is a security library distributed as a NuGet package.** Any default
   becomes every consuming application's staleness window on permission
   *revocation*. That belongs to each application as an opt-in decision.

If profiling later justifies it, the work is a separate issue with its own ADR:
opt-in flag, configurable TTL, eviction hooks on the write paths, and documented
multi-instance behavior.

### Rejected: Permissions As JWT Claims

Zero database reads per check, but the default access token lifetime is 540
minutes (README.md), which means a nine-hour revocation lag — worse than any
cache TTL under consideration. The library also deliberately serves permissions
through an endpoint (`AuthenticationController`) rather than through the token.
Not adopted.

## Components

| Unit | Responsibility | Depends on |
|---|---|---|
| `RequirePermissionAttribute` | `AuthorizeAttribute` subclass; sets `Policy` to `"Permission:<tag>"` | none |
| `PermissionRequirement` | `IAuthorizationRequirement` carrying `Tag` | none |
| `PermissionAuthorizationPolicyProvider` | Parses the `"Permission:"` prefix, builds the dynamic policy, delegates every other name to `DefaultAuthorizationPolicyProvider` | `IOptions<AuthorizationOptions>` |
| `PermissionAuthorizationHandler` | Applies the authorization rule table | `IHttpContextAccessor`, `IServiceScopeFactory` (fallback only) |
| `ScopedPermissionCache` | Per-request memoization of the merged set, keyed by user id | `IUserPermissionService` |
| `AddPermissionBasedAuthorization()` | Registration extension | — |

The policy prefix `"Permission:"` avoids collision with the existing named
policies in `SecurityPolicy`.

### Naming Note

The scratchpad proposed `UseRoleBasedAuthorizationHandler()`. That name is a
misnomer for a permission gate; the registration extension is
`AddPermissionBasedAuthorization()`, matching the `Add*` convention used by the
sibling authorization extensions.

## Scoped Access From A Singleton Handler

`IAuthorizationPolicyProvider` and `IAuthorizationHandler` are both registered as
singletons to avoid a captive dependency in the policy provider pipeline. The
handler must therefore reach scoped services without constructor injection.

**It resolves them from the request scope, not from a new child scope.** The
scratchpad proposed `IServiceScopeFactory.CreateAsyncScope()` per check, but that
creates a fresh scope every time, which would give a fresh `ScopedPermissionCache`
every time and memoize nothing. It would also open a second `DbContext` alongside
the request's own.

```csharp
var services = httpContextAccessor.HttpContext?.RequestServices;
if (services is not null)
{
    var cache = services.GetRequiredService<ScopedPermissionCache>();
    // ...
}
```

When `HttpContext` is null — the handler is invoked outside an HTTP request — fall
back to `scopeFactory.CreateAsyncScope()`. In that path the cache is per-check
rather than per-request, which is acceptable because there is no request to scope
to. The handler keeps `IServiceScopeFactory` injected for this fallback only.

`UserPermissionRepository` resolves its context through
`IScopedDbContextProvider<TUser>` in its constructor, so it must be resolved from
a live scope rather than captured in a singleton field.

## Cancellation

`HandleRequirementAsync` exposes no `CancellationToken`. The request token is
read from `IHttpContextAccessor`:

```csharp
var cancellationToken = httpContextAccessor.HttpContext?.RequestAborted
    ?? CancellationToken.None;
```

## Async Permission Path

`IUserPermissionService.HasPermission` is synchronous, so an authorization check
blocks a request thread on two database reads. The gate adds an async path.

```csharp
Task<bool> HasPermissionAsync(Guid userId, string permissionTag, CancellationToken cancellationToken);
Task<List<UserMergedPermission>> GetPermissionsForUserAsync(Guid userId, CancellationToken cancellationToken);
```

All projects target a single `net10.0` TFM, so these are added to the existing
`IUserPermissionService` as **default interface implementations** that delegate to
the synchronous members. External implementors are unaffected — this resolves the
scratchpad's open question without introducing a second interface.

`UserPermissionService` overrides both with genuinely asynchronous
implementations.

### Repository Additions

`IUserPermissionRepository.GetForUser` returns an already-materialized
`List<UserPermission>`, and `IGroupPermissionRepository.GetForUser` returns
`IQueryable<GroupPermission>`. Awaiting either from the service would require
`UserPermissionService` to reference `Microsoft.EntityFrameworkCore`, which it
currently does not.

Async methods are therefore added to the repositories, which already reference
EF Core, preserving the existing layering:

```csharp
// IUserPermissionRepository
Task<List<UserPermission>> GetForUserAsync(Guid userId, CancellationToken cancellationToken);

// IGroupPermissionRepository
Task<List<GroupPermission>> GetForUserAsync(Guid userId, CancellationToken cancellationToken);
```

## Registration

```csharp
services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
services.AddScoped<ScopedPermissionCache>();
```

`AddPermissionBasedAuthorization()` is opt-in and composes with the existing
`AddAuthorizationFor*` extensions rather than replacing any of them.

## Files

### New

| File | Purpose |
|---|---|
| `Cause.SecurityManagement.Core/Authentication/PermissionRequirement.cs` | Requirement carrying the tag |
| `Cause.SecurityManagement.Core/Authentication/PermissionAuthorizationHandler.cs` | Authorization rule |
| `Cause.SecurityManagement.Core/Authentication/PermissionAuthorizationPolicyProvider.cs` | Dynamic policy creation |
| `Cause.SecurityManagement.Core/Authentication/ScopedPermissionCache.cs` | Per-request memoization |
| `Cause.SecurityManagement.Core/RequirePermissionAttribute.cs` | The attribute |
| `Cause.SecurityManagement.Tests/Authentication/PermissionAuthorizationHandlerTests.cs` | Unit tests |
| `Cause.SecurityManagement.Tests/Authentication/PermissionAuthorizationPolicyProviderTests.cs` | Unit tests |
| `Cause.SecurityManagement.Tests/Authentication/ScopedPermissionCacheTests.cs` | Unit tests |
| `Cause.SecurityManagement.Integration.Tests/Authentication/RequirePermissionEndpointTests.cs` | Integration tests |

### Modified

| File | Change |
|---|---|
| `Cause.SecurityManagement.Core/Authentication/ServiceCollectionAuthorizationExtensions.cs` | Add `AddPermissionBasedAuthorization()` |
| `Cause.SecurityManagement.Core/Services/IUserPermissionService.cs` | Add async members as default implementations |
| `Cause.SecurityManagement.Core/Services/UserPermissionService.cs` | Async implementations |
| `Cause.SecurityManagement.Core/Repositories/IUserPermissionRepository.cs` | Add `GetForUserAsync` |
| `Cause.SecurityManagement.Core/Repositories/UserPermissionRepository.cs` | Implement `GetForUserAsync` |
| `Cause.SecurityManagement.Core/Repositories/IGroupPermissionRepository.cs` | Add `GetForUserAsync` |
| `Cause.SecurityManagement.Core/Repositories/GroupPermissionRepository.cs` | Implement `GetForUserAsync` |
| `README.md` | Document the attribute and the registration extension |

## Tests

Framework: NUnit, NSubstitute, AwesomeAssertions — matching the existing suites.

### Handler Unit Tests

| Case | Expected |
|---|---|
| `RegularUser` holding the permission | succeeds |
| `RegularUser` without the permission | does not succeed |
| `RegularUser` with the tag present but `Access` false | does not succeed |
| `Administrator` | succeeds, and the permission service is never called |
| `ExternalSystem` | does not succeed, no database call |
| `Console` | does not succeed, no database call |
| Each temporary role | does not succeed, no database call |
| No role claim | does not succeed |
| Missing `Sid` claim | does not succeed, no exception |
| Unparseable `Sid` claim | does not succeed, no exception |
| Request already cancelled | `OperationCanceledException` propagates |
| `HttpContext` is null | falls back to a child scope and still evaluates correctly |

### Policy Provider Unit Tests

| Case | Expected |
|---|---|
| `"Permission:CanEditBuilding"` | policy containing `PermissionRequirement` with that tag and `DenyAnonymousAuthorizationRequirement` |
| An existing policy name such as `SecurityPolicy.ExternalSystem` | delegated unchanged to the default provider |
| Unknown name without the prefix | delegated to the default provider |
| Configured fallback policy names schemes | dynamic policy carries the same scheme list |

### Cache Unit Tests

| Case | Expected |
|---|---|
| Two checks for the same user in one scope | permission service called once |
| Checks for two different users in one scope | permission service called once per user |
| Same user across two scopes | permission service called once per scope |

### Integration Tests

These cover the default-policy-stands-down hazard and cannot be replaced by unit
tests.

| Case | Expected |
|---|---|
| Unauthenticated request to a `[RequirePermission]` endpoint | 401 |
| `RegularUser` holding the permission, under each scheme configured by `AddAuthorizationForRegularUserKeycloakAndApiCertificate` | 200 |
| `RegularUser` without the permission | 403 |
| `Administrator` | 200 |
| `ExternalSystem` token | 403 |
| Endpoint with no `[RequirePermission]` in the same application | fallback policy still applies |

## Out Of Scope

* Cross-request or distributed permission caching.
* Permissions as JWT claims.
* Any change to `PermissionMergeTool` semantics.
* Any change to the existing `AddAuthorizationFor*` extensions beyond adding the
  new one.
* Permission gating for `ExternalSystem` or `Console` principals.

## Verification

Per AGENTS.md: no warnings, no errors, all unit and integration tests green.
