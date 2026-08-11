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
  permission attribute maps to a `"Permission:..."` policy created on demand. The
  policy requires an authenticated user and carries the application's configured
  authentication schemes, but does not require a role; role differentiation lives
  in the handler.
* **Option B**: A static policy per permission, registered at startup. Requires
  every application to enumerate its permission tags in `Program.cs` and keep
  that list in sync with the database.
* **Option C**: Permissions embedded as JWT claims, checked with
  `RequireClaim`. No database read per check.
* **Option D**: An MVC action filter rather than an authorization handler.

On attribute naming, considered separately:

* **Naming 1**: A single `[RequirePermission(tag)]`, with the Administrator bypass
  documented in the handler and this ADR.
* **Naming 2**: Two attributes whose names state the passing principals —
  `[AdministratorOrUserWithPermission(tag)]` and `[UserWithPermission(tag)]`.

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

On naming, chosen option: **Naming 2**. A single `[RequirePermission]` hides the
Administrator bypass from anyone reading the controller, and the codebase already
favours explicit descriptive attribute names
(`OpenToExternalSystemWithCertificateAttribute`,
`AuthorizeForUserAndAdministratorRolesAttribute`). The second attribute is not
speculative generality: applications that do not use Keycloak have no
`Administrator` principals, so naming one in their endpoint attributes would be
misleading. Nothing has shipped under the `RequirePermission` name, so there is no
compatibility cost to the rename.

### Authorization Rule

Only `RegularUser` principals are gated by permissions. Every other principal
type keeps its current behavior.

Two attributes make the passing principals explicit at the call site rather than
hiding the Administrator bypass in the handler:

| Attribute | `Administrator` | `RegularUser` | Everyone else |
|---|---|---|---|
| `[AdministratorOrUserWithPermission(tag)]` | pass, no database read | pass only if the merged set allows the tag | fail (403) |
| `[UserWithPermission(tag)]` | fail (403) | pass only if the merged set allows the tag | fail (403) |

One requirement type and one handler serve both, differing only by an
`AllowAdministrator` flag.

### Administrator And RegularUser Are Mutually Exclusive

This constrains the design and is easy to misread, so it is recorded here.
`BaseAuthenticator.GetSecurityRole` issues only `SecurityRoles.User`;
`MultiJwtClaimsTransformer` grants `Administrator` to every
Keycloak-authenticated principal with no `RegularUser` role alongside it; and
`TokenGenerator` writes one role claim per token.

In this codebase `Administrator` therefore means **"authenticated through
Keycloak"**, and such a principal's `Sid` need not correspond to a `User` row.
That is why the bypass is necessary rather than merely convenient: there are
generally no permission rows to evaluate for a Keycloak principal.

The practical guidance follows directly. Keycloak applications use
`[AdministratorOrUserWithPermission]`. Applications without Keycloak have no
`Administrator` principals at all, so `[UserWithPermission]` is the honest name
and both attributes behave identically.

### Permission Tags Stay Strings

`ModulePermission.Tag` is a string column and tags are database rows defined per
application, so the library cannot ship an enumeration of them. Consuming
applications obtain compile-time safety by declaring their own `const string`
members, which satisfy the compile-time-constant requirement for attribute
arguments. No library type is introduced for this.

An opt-in startup validation compares the tags found on registered endpoints
against `IPermissionCatalogService` and logs a warning per unknown tag. It
deliberately warns rather than throwing: a shared database missing a row must not
take a 9-1-1 application down at boot, the database may not be migrated yet at
startup, and the gate already fails closed.

### Interaction With The Default Authorization Convention

Because both permission attributes derive from `AuthorizeAttribute`,
`AuthorizationPolicy.CombineAsync` no longer consults
`AuthorizationOptions.FallbackPolicy` on a decorated endpoint — it does so only
when an endpoint carries no authorize data at all. The library's existing
`AuthorizeByRolesAttribute` family already had this property, so it is not new.

**The dynamic policy therefore inherits the fallback policy in full** — its
requirements and its authentication schemes — and adds the permission requirement
on top, via `AuthorizationPolicyBuilder.Combine`. A permission attribute may only
make an endpoint stricter, never looser.

An earlier revision of this decision copied only the authentication scheme list and
discarded the fallback policy's requirements. Security review found that widened
access under `AddAuthorizationForKeycloakAndRegularUserSchemes`, whose fallback
requires role `Administrator` only: a `RegularUser` holding the tag would pass an
endpoint the application's baseline denies. It also silently dropped any
consumer-defined fallback requirement such as tenant scoping or an MFA-completed
check. Full inheritance closes both.

### Correction: The MVC Filter Is Unioned, Not Suppressed

An earlier revision claimed the attributes suppress
`UseDefaultAuthorizationWhenNotSpecifiedFilter`. That is wrong.
`AuthorizationApplicationModelProvider` returns early when
`MvcOptions.EnableEndpointRouting` is true, which is the default, so authorization
attributes never become filters and that filter's guard never trips. It runs, and
`AuthorizeFilter.GetEffectivePolicyAsync` **unions** its policy with the endpoint's
metadata — a union of requirements being an AND.

Applications using `AskForAuthorizationByDefault` or
`AddAuthorizeFiltersControllerConvention` therefore AND the legacy
`"defaultpolicy"` (`RequireRole(SecurityRoles.User)`) with the permission policy,
which **denies every Keycloak Administrator** on decorated endpoints. This fails
closed, but it presents as a broken gate, and the tempting remedy — loosening the
legacy policy — is the dangerous one. The HTTP pipeline tests must cover both
filter-based registrations and the outcome must be documented.

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
* Good: The attribute name states which principals pass, so a reviewer reading a
  controller sees the Administrator bypass without consulting the handler.
* Good: The gate is opt-in and additive. Existing consumers are unaffected until
  they call `AddPermissionBasedAuthorization()`.
* Good: Role logic is concentrated in one testable handler instead of scattered
  through controller actions.
* Good: No revocation staleness window is introduced.
* Bad: Either permission attribute suppresses the fallback policy on the endpoint
  it decorates. The dynamic policy compensates, but the coupling is subtle and must
  stay covered by integration tests.
* Bad: Each gated endpoint adds a database read for `RegularUser` principals.
  Accepted for now; revisit only with profiling data.
* Bad: `IUserPermissionService` grows async members. Default interface
  implementations keep this non-breaking, but the interface now has two ways to
  ask the same question.
* Bad: The handler is a singleton reaching scoped services through
  `HttpContext.RequestServices`, which is correct but less obvious than
  constructor injection, and needs a documented fallback for the non-HTTP case.
* Bad: `[UserWithPermission]` denies every Keycloak-authenticated principal,
  because they hold `Administrator` and not `RegularUser`. Invisible in a
  non-Keycloak application, a hard lockout in a Keycloak one. Mitigated by a
  prominent README warning and an integration test pinning the behavior, but the
  shorter name will still read as the more general one to some developers.
* Bad: This design depends on `MultiJwtClaimsTransformer` granting `Administrator`
  to Keycloak principals — behavior carrying an unresolved maintainer comment
  questioning it. If that changes, both attributes must be revisited.

## Maintenance Invariants
<!-- Behaviors to preserve; this decision is implemented -->
- The handler must fail closed. It succeeds only on an explicit positive match.
- The dynamic policy must always include `RequireAuthenticatedUser()`, because the
  fallback policy does not run on decorated endpoints.
- The dynamic policy must inherit the application's `FallbackPolicy` in full. A
  permission attribute may only make an endpoint stricter, never looser. Copying
  only the authentication schemes reintroduces a privilege-widening gap.
- The dynamic policy must not add its own `RequireRole`. Role differentiation for
  the permission decision stays in the handler; what the policy inherits is
  whatever role requirement the application configured, which is a different thing.
- `AllowsCachingPolicies` must stay `true`. The interface default is `false`, and
  because this provider replaces `DefaultAuthorizationPolicyProvider` globally,
  the default would disable `AuthorizationPolicyCache` for every endpoint in the
  consuming application, not only permission-gated ones.
- The handler must call `context.Succeed(requirement)` on the requirement instance
  passed to it, never on everything in `context.Requirements`. Stacked attributes
  AND; succeeding them all would turn that into an OR and create a real bypass.
- The registration must not use `TryAddSingleton` for `IAuthorizationPolicyProvider`.
  `AddAuthorizationCore()` already registers the default provider that way, so a
  consumer calling an `AddAuthorizationFor*` helper first would make the
  registration a silent no-op and every permission-gated endpoint would fail.
- `PermissionRequirement` must not gain a `ToString()` override — the framework's
  "requirements were not met" log line would then emit the permission tag.
- `PermissionAuthorizationPolicyProvider` must delegate every policy name lacking
  the `"Permission:"` prefix to `DefaultAuthorizationPolicyProvider`, so existing
  named policies keep working.
- Maintain integration tests proving that a permission-gated endpoint still
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
- Maintain the test asserting that `Administrator` passes
  `[AdministratorOrUserWithPermission]` and is denied `[UserWithPermission]`.
  That contrast is the entire reason two attributes exist.
- A policy name carrying the `"Permission:"` prefix with an unrecognized mode
  segment must return no policy. Falling back to a weaker gate would turn a typo
  into a silent privilege escalation.
- Startup tag validation must never throw, and must log at warning level when it
  skips rather than failing silently.
- Maintain the end-to-end tests that seed real permission rows and assert real
  status codes through the gate. The HTTP-pipeline tests stub the permission
  service and the repository tests exercise no HTTP, so only this layer proves a
  `Sid` claim actually resolves to the rows the repositories filter on. A break
  there fails closed and presents as a data problem rather than a bug.

## Implementation Plan
<!-- Crucial section so Claude Code knows how to execute it -->
Full detail in [docs/specs/2026-08-11-permission-based-gate.md](../../specs/2026-08-11-permission-based-gate.md).

- [ ] Task 1: Add `PermissionRequirement` (`Tag`, `AllowAdministrator`), both
      attributes in `Cause.SecurityManagement.Core/PermissionAttributes.cs`, and
      `PermissionAuthorizationPolicyProvider` under
      `Cause.SecurityManagement.Core/Authentication/`.
- [ ] Task 2: Add async members to `IUserPermissionService` as default interface
      implementations, with real async overrides in `UserPermissionService`, plus
      `GetForUserAsync` on `IUserPermissionRepository`,
      `IGroupPermissionRepository`, and both implementations.
- [ ] Task 3: Add `ScopedPermissionCache` and `PermissionAuthorizationHandler`.
- [ ] Task 4: Add `AddPermissionBasedAuthorization(bool validateTagsAtStartup =
      false)` to `ServiceCollectionAuthorizationExtensions`, plus
      `PermissionTagValidationHostedService`. Document both attributes, the
      consumer-side `Permission` constants pattern, and the `[UserWithPermission]`
      Keycloak lockout warning in `README.md`.
- [ ] Task 5: Write unit tests for the handler, the policy provider, the cache,
      and tag validation, per the tables in the spec. The handler matrix runs
      against both `AllowAdministrator` values.
- [ ] Task 6: Write integration tests under `Cause.SecurityManagement.Integration.Tests`
      covering unauthenticated access, each configured authentication scheme, the
      denied cases, an undecorated endpoint in the same application, and the
      Keycloak-principal contrast between the two attributes.
- [ ] Task 7: Confirm the scheme-list hypothesis in the spec. If the integration
      tests show the scheme list is unnecessary, remove the copying and simplify
      the policy provider.
- [ ] Task 8: Build with no warnings, run the full unit and integration suites,
      and flip this ADR to `accepted`.
