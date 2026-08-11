# Spec: Permission-Based Authorization Gate

* Issue: #115
* Date: 2026-08-11
* Status: approved for implementation
* Related ADR: [2026-08-11-permission-based-authorization-gate.md](../adr/records/2026-08-11-permission-based-authorization-gate.md)

## Goal

Provide a granular, per-endpoint permission gate usable across all API patterns
supported by the library, with the passing principals **named at the call site**
rather than hidden in the handler.

```csharp
[AdministratorOrUserWithPermission(Permission.CanEditBuilding)]
public async Task<IActionResult> EditBuilding(...) { }
```

## Two Attributes

The attribute name states which principals pass. Both take a permission tag and
share one requirement type and one handler.

| Attribute | `Administrator` | `RegularUser` | Everyone else |
|---|---|---|---|
| `[AdministratorOrUserWithPermission(tag)]` | pass, no database read | pass only if the merged set allows the tag | fail (403) |
| `[UserWithPermission(tag)]` | **fail (403)** | pass only if the merged set allows the tag | fail (403) |

"Everyone else" means `ExternalSystem`, `Console` (`ApiCertificate`), each
temporary role (`UserCreation`, `UserRecovery`, `UserPasswordSetup`,
`UserLoginWithMultiFactor`), and any principal with no recognized role. None of
them incurs a database read.

The handler fails closed: it succeeds only on an explicit positive match and
otherwise leaves the requirement unsatisfied.

### Why Two Attributes, And When To Use Which

`Administrator` and `RegularUser` are **mutually exclusive** in this library:

* `BaseAuthenticator.GetSecurityRole` issues only `SecurityRoles.User`. The
  library's own login path never grants `Administrator`.
* `Administrator` is granted in exactly one place —
  `MultiJwtClaimsTransformer` — to every Keycloak-authenticated principal, with
  no `RegularUser` role alongside it.
* `TokenGenerator` writes a single role claim per token.

So in this codebase `Administrator` effectively means **"authenticated through
Keycloak"**, and such a principal's `Sid` need not correspond to a `User` row at
all. That is why the bypass exists: there are generally no permission rows to
check for a Keycloak principal.

It follows that:

* **Applications that use Keycloak** should use
  `[AdministratorOrUserWithPermission]`.
* **Applications that do not use Keycloak** have no `Administrator` principals at
  all, so `[UserWithPermission]` is the honest name and the two attributes behave
  identically.

> **Footgun, document prominently in the README.**
> `[UserWithPermission]` denies **every Keycloak-authenticated principal**,
> because they hold `Administrator` and not `RegularUser`. In a non-Keycloak
> application this is invisible and harmless. In a Keycloak application it is a
> hard lockout. Choosing the shorter name because it looks more general is a
> mistake.

### Referencing Tags From Consuming Code

The tag parameter is a `string`, because `ModulePermission.Tag` is a string
column and tags are database rows defined per application. The library cannot
ship an enumeration of them.

Consuming applications get compile-time safety by declaring their own constants;
attribute arguments only require compile-time constants, which `const string`
satisfies:

```csharp
public static class Permission
{
    public const string CanEditBuilding = "CanEditBuilding";
}

[AdministratorOrUserWithPermission(Permission.CanEditBuilding)]
public async Task<IActionResult> EditBuilding(...) { }
```

This needs no library support beyond keeping the parameter a `string`, and is
documented in the README as the recommended pattern.

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

Because both attributes derive from `AuthorizeAttribute`, decorating an endpoint
with either one **suppresses the application's configured fallback policy on that
endpoint**. The `Permission:` policy becomes the only gate.

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
* `PermissionRequirement(tag, allowAdministrator)`

The policy deliberately does **not** call `RequireRole`. Role differentiation
lives in the handler so that both attributes behave identically in
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
| `AdministratorOrUserWithPermissionAttribute` | `AuthorizeAttribute` subclass; sets `Policy` to `"Permission:AdministratorOrUser:<tag>"` | none |
| `UserWithPermissionAttribute` | `AuthorizeAttribute` subclass; sets `Policy` to `"Permission:User:<tag>"` | none |
| `PermissionRequirement` | `IAuthorizationRequirement` carrying `Tag` and `AllowAdministrator` | none |
| `PermissionAuthorizationPolicyProvider` | Parses the `"Permission:"` prefix, builds the dynamic policy, delegates every other name to `DefaultAuthorizationPolicyProvider` | `IOptions<AuthorizationOptions>` |
| `PermissionAuthorizationHandler` | Applies the rule table for both attributes | `IHttpContextAccessor`, `IServiceScopeFactory` (fallback only) |
| `ScopedPermissionCache` | Per-request memoization of the merged set, keyed by user id | `IUserPermissionService` |
| `PermissionTagValidationHostedService` | Opt-in startup validation of attribute tags | `EndpointDataSource`, `IServiceScopeFactory`, `ILogger` |
| `AddPermissionBasedAuthorization()` | Registration extension | — |

One requirement type and one handler serve both attributes; they differ only by
the `AllowAdministrator` flag. This avoids a duplicated handler and keeps the two
behaviors adjacent in one readable place.

### Policy Naming

The `"Permission:"` prefix avoids collision with the existing named policies in
`SecurityPolicy`. The second segment carries the mode:

| Attribute | Policy name |
|---|---|
| `[AdministratorOrUserWithPermission("CanEditBuilding")]` | `Permission:AdministratorOrUser:CanEditBuilding` |
| `[UserWithPermission("CanEditBuilding")]` | `Permission:User:CanEditBuilding` |

The provider splits on `:` **limited to three parts**, so a tag containing a
colon is preserved intact rather than truncated. A name with the `"Permission:"`
prefix but an unrecognized mode segment must return no policy, which surfaces as
an error rather than silently degrading to a weaker gate.

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

## Startup Tag Validation

A typo'd tag matches no `ModulePermission` row, so every `RegularUser` is denied
while Administrators still pass. That fails closed — safe — but presents as a
permissions-data problem rather than a code defect, and can reach production
unnoticed.

Opt-in validation catches it at startup:

```csharp
services.AddPermissionBasedAuthorization(validateTagsAtStartup: true);
```

`PermissionTagValidationHostedService` enumerates `EndpointDataSource.Endpoints`,
collects every `IAuthorizeData.Policy` beginning with `"Permission:"`, parses the
tags, compares them against `IPermissionCatalogService.GetPermissionsAsync()`, and
**logs one warning per unknown tag**.

Endpoint enumeration rather than assembly scanning is deliberate: it reads the
policy names the pipeline will actually use, and it covers MVC controllers and
Wolverine endpoints alike, since both register endpoints.

It never fails startup. Three reasons, and this is a deliberate rejection of the
stricter option:

1. A shared database missing a row would take the whole application down at boot,
   which is unacceptable for a 9-1-1 system.
2. The database may legitimately not be migrated yet at startup.
3. The gate already fails closed, so an unknown tag denies rather than grants.

The service must therefore wrap catalog access in a `try`/`catch`, and log that
validation was **skipped** — at warning level, never silently — if the catalog
cannot be read or `IPermissionCatalogService` is not registered.

## Registration

```csharp
services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
services.AddScoped<ScopedPermissionCache>();
// only when validateTagsAtStartup is true
services.AddHostedService<PermissionTagValidationHostedService>();
```

`AddPermissionBasedAuthorization()` is opt-in and composes with the existing
`AddAuthorizationFor*` extensions rather than replacing any of them.
`validateTagsAtStartup` defaults to `false`.

## Files

### New

| File | Purpose |
|---|---|
| `Cause.SecurityManagement.Core/Authentication/PermissionRequirement.cs` | Requirement carrying `Tag` and `AllowAdministrator` |
| `Cause.SecurityManagement.Core/Authentication/PermissionAuthorizationHandler.cs` | Authorization rule |
| `Cause.SecurityManagement.Core/Authentication/PermissionAuthorizationPolicyProvider.cs` | Dynamic policy creation |
| `Cause.SecurityManagement.Core/Authentication/ScopedPermissionCache.cs` | Per-request memoization |
| `Cause.SecurityManagement.Core/Authentication/PermissionTagValidationHostedService.cs` | Opt-in startup tag validation |
| `Cause.SecurityManagement.Core/PermissionAttributes.cs` | Both attributes, alongside the existing `AuthorizeByRolesAttribute.cs` pattern |
| `Cause.SecurityManagement.Tests/Authentication/PermissionAuthorizationHandlerTests.cs` | Unit tests |
| `Cause.SecurityManagement.Tests/Authentication/PermissionAuthorizationPolicyProviderTests.cs` | Unit tests |
| `Cause.SecurityManagement.Tests/Authentication/ScopedPermissionCacheTests.cs` | Unit tests |
| `Cause.SecurityManagement.Tests/Authentication/PermissionTagValidationHostedServiceTests.cs` | Unit tests |
| `Cause.SecurityManagement.Integration.Tests/Authentication/PermissionGateEndpointTests.cs` | Integration tests |

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
| `README.md` | Document both attributes, the `Permission` constants pattern, the registration extension, and the `[UserWithPermission]` Keycloak lockout warning |

## Tests

Framework: NUnit, NSubstitute, AwesomeAssertions — matching the existing suites.

### Handler Unit Tests

Every case runs against **both** `AllowAdministrator` values except where the
expectation differs, which is called out explicitly.

| Case | `AdministratorOrUserWithPermission` | `UserWithPermission` |
|---|---|---|
| `RegularUser` holding the permission | succeeds | succeeds |
| `RegularUser` without the permission | does not succeed | does not succeed |
| `RegularUser` with the tag present but `Access` false | does not succeed | does not succeed |
| `Administrator` | **succeeds, permission service never called** | **does not succeed, permission service never called** |
| `ExternalSystem` | does not succeed, no database call | does not succeed, no database call |
| `Console` | does not succeed, no database call | does not succeed, no database call |
| Each temporary role | does not succeed, no database call | does not succeed, no database call |
| No role claim | does not succeed | does not succeed |
| Missing `Sid` claim | does not succeed, no exception | does not succeed, no exception |
| Unparseable `Sid` claim | does not succeed, no exception | does not succeed, no exception |
| Request already cancelled | `OperationCanceledException` propagates | same |
| `HttpContext` is null | falls back to a child scope and still evaluates | same |

The `Administrator` row is the behavioral contract between the two attributes and
must be asserted directly, not inferred.

### Policy Provider Unit Tests

| Case | Expected |
|---|---|
| `"Permission:AdministratorOrUser:CanEditBuilding"` | policy with `PermissionRequirement { Tag = "CanEditBuilding", AllowAdministrator = true }` plus `DenyAnonymousAuthorizationRequirement` |
| `"Permission:User:CanEditBuilding"` | same, with `AllowAdministrator = false` |
| A tag containing a colon | tag preserved intact, not truncated |
| `"Permission:Bogus:CanEditBuilding"` | returns no policy (unrecognized mode must not degrade to a weaker gate) |
| An existing policy name such as `SecurityPolicy.ExternalSystem` | delegated unchanged to the default provider |
| Unknown name without the prefix | delegated to the default provider |
| Configured fallback policy names schemes | dynamic policy carries the same scheme list |

### Cache Unit Tests

| Case | Expected |
|---|---|
| Two checks for the same user in one scope | permission service called once |
| Checks for two different users in one scope | permission service called once per user |
| Same user across two scopes | permission service called once per scope |

### Tag Validation Unit Tests

| Case | Expected |
|---|---|
| All attribute tags exist in the catalog | no warning logged |
| One tag missing from the catalog | exactly one warning naming that tag; startup still succeeds |
| Catalog access throws | a "validation skipped" warning; startup still succeeds |
| `IPermissionCatalogService` not registered | a "validation skipped" warning; startup still succeeds |

### Integration Tests

These cover the default-policy-stands-down hazard and cannot be replaced by unit
tests.

| Case | Expected |
|---|---|
| Unauthenticated request to a gated endpoint | 401 |
| `RegularUser` holding the permission, under each scheme configured by `AddAuthorizationForRegularUserKeycloakAndApiCertificate` | 200 |
| `RegularUser` without the permission | 403 |
| Keycloak principal on `[AdministratorOrUserWithPermission]` | 200 |
| Keycloak principal on `[UserWithPermission]` | 403 — pins the documented lockout so it cannot regress unnoticed |
| `ExternalSystem` token | 403 |
| Endpoint with no permission attribute in the same application | fallback policy still applies |

## Out Of Scope

* Cross-request or distributed permission caching.
* Permissions as JWT claims.
* Any change to `PermissionMergeTool` semantics.
* Any change to the existing `AddAuthorizationFor*` extensions beyond adding the
  new one.
* Permission gating for `ExternalSystem` or `Console` principals.
* Failing startup on an unknown tag; validation warns only.
* The unresolved maintainer comment at `MultiJwtClaimsTransformer.cs:32` about
  whether Keycloak principals should receive `Administrator`, and the
  `"Administrator"` string literal on line 34 that should be
  `SecurityRoles.Administrator`. Both are pre-existing and unrelated to this
  issue, but this design depends on that behavior, so a future change there must
  revisit this spec.

## Verification

Per AGENTS.md: no warnings, no errors, all unit and integration tests green.
