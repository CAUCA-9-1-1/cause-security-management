# Permission-Based Authorization Gate For Regular Users

* Status: proposed
* Date: 2026-08-11
* Deciders: Cause.SecurityManagement maintainers
* Technical Story: Issue #115

## Context and Problem Statement

Authorization in the library is role-based. Consuming applications can require
`RegularUser`, `Administrator`, `ExternalSystem`, or `Console`, but they cannot
express a finer-grained rule such as "this endpoint requires the
`CanEditBuilding` permission". Applications needing that granularity implement it
by hand inside controller actions, which is inconsistent across projects, easy to
forget on a new endpoint, and untested.

The permission data already exists (`UserPermission`, `GroupPermission`, and the
merge in `PermissionMergeTool`) and is already exposed to clients through
`AuthenticationController`. What is missing is an enforcement point in the
authorization pipeline.

A complication constrains the design. The repository's default authorization
convention, recorded in
[2026-06-04-default-authorization-and-controller-conventions.md](2026-06-04-default-authorization-and-controller-conventions.md),
applies the fallback policy through `UseDefaultAuthorizationWhenNotSpecifiedFilter`,
which stands down whenever another authorization filter is present on the
endpoint. Any attribute deriving from `AuthorizeAttribute` therefore suppresses
the application's configured fallback policy on the endpoint it decorates.

## Decision Drivers

* Express per-endpoint permission requirements declaratively.
* Fail closed. A misconfigured or unrecognized principal must be denied.
* Preserve the secure-by-default guarantee of the existing convention rather than
  quietly punching a hole in it.
* Work unchanged in both `RegularUser`-default and `Administrator`-default
  applications.
* Avoid a staleness window on permission revocation in a security library
  consumed by multiple applications.
* Keep the change additive and opt-in for existing consumers.

## Considered Options

* **Option A**: A dynamic policy provider plus an authorization handler. A
  `[RequirePermission("tag")]` attribute maps to a `"Permission:<tag>"` policy
  created on demand. The policy requires an authenticated user and carries the
  application's configured authentication schemes, but does not require a role;
  role differentiation lives in the handler.
* **Option B**: A static policy per permission, registered at startup. Requires
  every application to enumerate its permission tags in `Program.cs` and keep
  that list in sync with the database.
* **Option C**: Permissions embedded as JWT claims, checked with
  `RequireClaim`. No database read per check.
* **Option D**: An MVC action filter rather than an authorization handler.

## Decision Outcome

Chosen option: **Option A**, because it is the only option that requires no
per-application registration of tags, integrates with the standard authorization
pipeline, and keeps role handling in one place that can be unit-tested directly.

Option B was rejected because the tag list lives in the database and duplicating
it in startup code guarantees drift.

Option C was rejected because the default access token lifetime is 540 minutes,
which turns a permission revocation into a nine-hour lag — strictly worse than
any caching window under consideration. It also contradicts the existing design,
which serves permissions through an endpoint rather than the token.

Option D was rejected because action filters run after authorization, produce
inconsistent status codes relative to the rest of the pipeline, and cannot
participate in policy composition.

### Authorization Rule

Only `RegularUser` principals are gated by permissions. Every other principal
type keeps its current behavior.

| Principal | Result |
|---|---|
| `Administrator` | pass, without a database read |
| `RegularUser` | pass only if the merged permission set allows the tag |
| `ExternalSystem`, `Console`, temporary roles, no recognized role | fail (403) |

### Interaction With The Default Authorization Convention

Because `RequirePermissionAttribute` derives from `AuthorizeAttribute`, the
fallback policy does not run on a decorated endpoint. The dynamic policy
therefore carries `RequireAuthenticatedUser()` and the fallback policy's
authentication scheme list itself, and the handler verifies the role positively.
This is a security requirement of the design, not an implementation preference.

### Caching

Permission lookups are memoized **per request only**, keyed by user id. Two
database reads per check are collapsed to one per request, with no staleness
window and no invalidation logic.

Cross-request and distributed caching are explicitly deferred. The merged
permission set derives from both user-level and group-level rows, so invalidation
fans out across users rather than evicting a single key; in-memory eviction does
not propagate across horizontally scaled instances, leaving correctness dependent
on a TTL alone; and as a distributed NuGet package, any default TTL becomes every
consuming application's revocation lag. If profiling justifies it later, it
warrants its own issue and ADR.

### Consequences

* Good: Endpoints express permission requirements declaratively and consistently
  across consuming applications.
* Good: The gate is opt-in and additive. Existing consumers are unaffected until
  they call `AddPermissionBasedAuthorization()`.
* Good: Role logic is concentrated in one testable handler instead of scattered
  through controller actions.
* Good: No revocation staleness window is introduced.
* Bad: `[RequirePermission]` suppresses the fallback policy on the endpoint it
  decorates. The dynamic policy compensates, but the coupling is subtle and must
  stay covered by integration tests.
* Bad: Each gated endpoint adds a database read for `RegularUser` principals.
  Accepted for now; revisit only with profiling data.
* Bad: `IUserPermissionService` grows async members. Default interface
  implementations keep this non-breaking, but the interface now has two ways to
  ask the same question.
* Bad: The handler is a singleton reaching scoped services through
  `HttpContext.RequestServices`, which is correct but less obvious than
  constructor injection, and needs a documented fallback for the non-HTTP case.

## Maintenance Invariants
<!-- Behaviors to preserve; this decision is implemented -->
- The handler must fail closed. It succeeds only on an explicit positive match.
- The dynamic policy must always include `RequireAuthenticatedUser()`, because the
  fallback policy does not run on decorated endpoints.
- The dynamic policy must not call `RequireRole`. Role differentiation stays in
  the handler so the attribute behaves identically regardless of the
  application's default role set.
- `PermissionAuthorizationPolicyProvider` must delegate every policy name lacking
  the `"Permission:"` prefix to `DefaultAuthorizationPolicyProvider`, so existing
  named policies keep working.
- Maintain integration tests proving that a `[RequirePermission]` endpoint still
  rejects unauthenticated callers and still accepts every authentication scheme
  the application has configured.
- Maintain unit tests proving `Administrator` succeeds without any permission
  lookup, and that `ExternalSystem`, `Console`, and each temporary role are
  denied.
- Permission caching stays request-scoped. Introducing a cross-request cache
  requires a new ADR addressing invalidation fan-out and multi-instance eviction.
- The handler must resolve scoped services from `HttpContext.RequestServices` so
  the cache spans the request. Creating a child scope per authorization check
  would silently reduce the cache to per-check and defeat it.

## Implementation Plan
<!-- Crucial section so Claude Code knows how to execute it -->
Full detail in [docs/specs/2026-08-11-permission-based-gate.md](../../specs/2026-08-11-permission-based-gate.md).

- [ ] Task 1: Add `PermissionRequirement`, `RequirePermissionAttribute`, and
      `PermissionAuthorizationPolicyProvider` under
      `Cause.SecurityManagement.Core/Authentication/`.
- [ ] Task 2: Add async members to `IUserPermissionService` as default interface
      implementations, with real async overrides in `UserPermissionService`, plus
      `GetForUserAsync` on `IUserPermissionRepository`,
      `IGroupPermissionRepository`, and both implementations.
- [ ] Task 3: Add `ScopedPermissionCache` and `PermissionAuthorizationHandler`.
- [ ] Task 4: Add `AddPermissionBasedAuthorization()` to
      `ServiceCollectionAuthorizationExtensions`, and document the attribute in
      `README.md`.
- [ ] Task 5: Write unit tests for the handler, the policy provider, and the
      cache, per the tables in the spec.
- [ ] Task 6: Write integration tests under `Cause.SecurityManagement.Integration.Tests`
      covering unauthenticated access, each configured authentication scheme, the
      denied cases, and an undecorated endpoint in the same application.
- [ ] Task 7: Confirm the scheme-list hypothesis in the spec. If the integration
      tests show the scheme list is unnecessary, remove the copying and simplify
      the policy provider.
- [ ] Task 8: Build with no warnings, run the full unit and integration suites,
      and flip this ADR to `accepted`.
