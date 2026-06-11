# Scratchpad: Permission-Based Authorization Handler

## Goal

Add a granular, per-endpoint permission gate usable across all API patterns.

```csharp
[RequirePermission("CanEditBuilding")]
public async Task<IActionResult> EditBuilding(...) {}
```

Administrators always pass. RegularUsers pass only if they hold the named permission.  
ExternalSystem, ApiCertificate, and temporary roles are implicitly blocked.

---

## New Files

| File | Purpose |
|------|---------|
| `Cause.SecurityManagement.Core/Authentication/PermissionRequirement.cs` | `IAuthorizationRequirement` carrying the permission `Tag` |
| `Cause.SecurityManagement.Core/Authentication/PermissionAuthorizationHandler.cs` | `AuthorizationHandler<PermissionRequirement>` — core logic |
| `Cause.SecurityManagement.Core/Authentication/PermissionAuthorizationPolicyProvider.cs` | `IAuthorizationPolicyProvider` — creates dynamic policies on-the-fly |
| `Cause.SecurityManagement.Core/RequirePermissionAttribute.cs` | `AuthorizeAttribute` subclass with `"Permission:"` prefix |
| `Cause.SecurityManagement.Tests/Authentication/PermissionAuthorizationHandlerTests.cs` | NUnit unit tests |

## Modified Files

| File | Change |
|------|--------|
| `Cause.SecurityManagement.Core/Authentication/ServiceCollectionAuthorizationExtensions.cs` | Add `UseRoleBasedAuthorizationHandler()` extension |

---

## Design Decisions

### Attribute & Policy Naming

- Attribute: `[RequirePermission("tag")]` — signals "permission gate", not role gate.
- Policy prefix: `"Permission:"` — avoids collision with existing named policies
  (`UserRecovery`, `ExternalSystem`, `ApiCertificate`, etc.).
- `PolicyProvider` delegates unknown policy names to `DefaultAuthorizationPolicyProvider`
  so existing policies are unaffected.

### Handler Role Logic

The dynamic policy does **not** `RequireRole` — the handler differentiates internally:

```
if user has Administrator role  → Succeed (no DB call)
if user has RegularUser role    → load permissions → Succeed if HasPermission(tag)
otherwise                       → do nothing (implicit 403)
```

This makes `[RequirePermission]` safe to use standalone in both RegularUser-default
and Administrator-default APIs without attribute stacking issues.

### Scoped DB Access

`IAuthorizationHandler` is registered as **Singleton** to avoid captive dependency
issues with the policy provider pipeline. To safely call the scoped
`IUserPermissionService` (which depends on a scoped DbContext), the handler
creates an explicit child scope per authorization check via `IServiceScopeFactory`:

```csharp
await using var scope = _scopeFactory.CreateAsyncScope();
var permissionService = scope.ServiceProvider.GetRequiredService<IUserPermissionService>();
```

This also makes the dependency chain explicit and testable.

### Cancellation

`HandleRequirementAsync` does not expose a `CancellationToken` on its signature.
The request cancellation token is obtained from `IHttpContextAccessor`:

```csharp
var cancellationToken = _httpContextAccessor.HttpContext?.RequestAborted
    ?? CancellationToken.None;
```

`IUserPermissionService` must expose an async variant accepting a `CancellationToken`
(or the repository layer must), so that the DB call can be cancelled on client
disconnect. This may require adding an async `HasPermissionAsync(Guid, string, CancellationToken)`
overload to `IUserPermissionService` and its implementation.

### Registration

```csharp
services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
```

Both Singleton — the handler uses `IServiceScopeFactory` for scoped access,
so there is no captive dependency.

---

## Handler Sketch

```csharp
protected override async Task HandleRequirementAsync(
    AuthorizationHandlerContext context,
    PermissionRequirement requirement)
{
    if (context.User.IsInRole(SecurityRoles.Administrator))
    {
        context.Succeed(requirement);
        return;
    }

    if (!context.User.IsInRole(SecurityRoles.User))
        return;

    var sidClaim = context.User.FindFirstValue(JwtRegisteredClaimNames.Sid);
    if (!Guid.TryParse(sidClaim, out var userId))
        return;

    var cancellationToken = _httpContextAccessor.HttpContext?.RequestAborted
        ?? CancellationToken.None;

    await using var scope = _scopeFactory.CreateAsyncScope();
    var permissionService = scope.ServiceProvider
        .GetRequiredService<IUserPermissionService>();

    if (await permissionService.HasPermissionAsync(userId, requirement.Tag, cancellationToken))
        context.Succeed(requirement);
}
```

---

## Impact on IUserPermissionService

Current `HasPermission(Guid, string)` is synchronous. A new overload is needed:

```csharp
Task<bool> HasPermissionAsync(Guid userId, string permissionTag, CancellationToken cancellationToken);
```

This also requires an async path in `IUserPermissionRepository`:

```csharp
Task<List<UserMergedPermission>> GetPermissionsForUserAsync(Guid userId, CancellationToken cancellationToken);
```

---

## Tests (NUnit + NSubstitute + AwesomeAssertions)

| Test | Expected |
|------|---------|
| RegularUser with matching permission | `context.HasSucceeded == true` |
| RegularUser without matching permission | `context.HasSucceeded == false` |
| Administrator | `context.HasSucceeded == true` (no DB call) |
| ExternalSystem | `context.HasSucceeded == false` |
| Missing / unparseable Sid claim | `context.HasSucceeded == false`, no exception |
| Cancelled request (token cancelled) | `OperationCanceledException` propagates |

---

## Open Questions

- Should `IUserPermissionService.HasPermissionAsync` be added to the existing
  interface (breaking change for existing implementors) or as a new interface
  `IAsyncUserPermissionService`? Recommendation: extend the existing interface
  with a default implementation that wraps the sync version, making it
  non-breaking.
- TTL for a future in-memory cache layer: 1 minute is a reasonable default.
  Cache key: `userId`. Invalidation: on permission change (explicit eviction from
  the management service). Not in scope for this iteration.
