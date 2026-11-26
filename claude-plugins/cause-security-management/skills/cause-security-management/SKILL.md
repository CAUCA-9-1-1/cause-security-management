---
name: cause-security-management
description: >
  Cause.SecurityManagement, the Cauca authentication and authorization library for ASP.NET Core
  (net10.0 and net11.0): JWT login for internal users, Keycloak, console and external-system
  certificates, fallback authorization policies, the permission gate
  ([AdministratorOrUserWithPermission], [UserWithPermission]), and the user, group and permission
  management controllers. Use it whenever code references a Cause.SecurityManagement package, calls
  InjectSecurityServices, AddTokenAuthentication, AddAuthorizationFor*, AddPermissionBasedAuthorization
  or AddSecurityManagementHandlers, subclasses BaseSecurityContext or a Base*Controller of the library,
  or when someone asks to secure, protect or restrict an endpoint by permission in a Cauca API — even if
  the library is not named. Triggers: "sécuriser un endpoint", "ajouter une permission",
  "UserWithPermission", "Keycloak", "certificat console", "système externe", "403 inattendu",
  "gestion des usagers", "Cause.SecurityManagement".
---

# Cause.SecurityManagement

Packages 11.0.0, `net10.0` and `net11.0`. Source: the `cause-security-management` repository
(GitHub `CAUCA-9-1-1`). Every fact below was checked against the 11.0.0 source on 2026-11-26.

| Package | Content | Published |
|---|---|---|
| `Cause.SecurityManagement.Models` | Entities (`User`…), `SecurityConfiguration`, `KeycloakConfiguration`, DTOs | yes |
| `Cause.SecurityManagement.Core` | `InjectSecurityServices`, `AddAuthorizationFor*`, permission gate, roles, policies, EF context and mappings | yes |
| `Cause.SecurityManagement` | MVC controllers, `AddTokenAuthentication*`, external-system authentication, MVC conventions | yes |
| `Cause.SecurityManagement.Wolverine.ExternalSystem` | Wolverine endpoints for external-system logon/refresh, plus `GET /api/Me` | yes |
| `Cause.SecurityManagement.Wolverine` | Full Wolverine endpoints and sagas | **no** — `IsPackable=false`, `10.2.0-experimental5` |

The repository README is partly stale. Where it disagrees with this skill, the source wins: for
example `InjectSecurityServices` takes one type argument, the default access token lasts 60 minutes
and the refresh token 540, and `AllowRefreshWithExpiredToken` no longer exists.

Reference implementations: `customer-portal` (`webapi/Cause.CustomerPortal.WebApi/Extensions/DependencyInjectionExtensions.cs`,
`RegisterSecurity`) and `mass-alert` (`webapi/Cauca.MassAlert.WebApi/Startup.cs`, `ConfigureSecurity`).
Both combine `AddTokenAuthentication`, a hand-built `FallbackPolicy` and `AddPermissionBasedAuthorization`.

## Where to read next

| Task | Read |
|---|---|
| Wire the library into a new API, `appsettings`, DbContext, options of `InjectSecurityServices` | [references/setup.md](references/setup.md) |
| Choose an authentication scheme or an `AddAuthorizationFor*` baseline; Keycloak, certificates, external systems; roles and policy names | [references/authentication-and-authorization.md](references/authentication-and-authorization.md) |
| Restrict an endpoint by permission tag; diagnose an unexpected 403 | [references/permission-gate.md](references/permission-gate.md) |
| Expose user, group or permission management screens (`@cauca-911/cauca-management`) | [references/management-controllers.md](references/management-controllers.md) |
| Host on Wolverine HTTP instead of MVC | [references/wolverine.md](references/wolverine.md) |

## Facts that bite

These cause silent security holes or production 401/403s. Check each one against the code you write
or review.

**Setup**
- `BaseSecurityContext.OnModelCreating` does not apply the library's EF mappings. Call
  `AddSecurityManagementMappings(modelBuilder)` in your override.
- `EnsureAdminIsCreated()` seeds user `admin` with a password hardcoded in the library. Never let that
  account reach a networked environment unchanged.
- Rely on an `AddAuthorizationFor*` `FallbackPolicy` for "authenticated by default". Do not add the
  MVC filter `AskForAuthorizationByDefault` to new code: under endpoint routing it is ANDed with every
  endpoint's own requirement and refuses Keycloak Administrators and MFA or recovery tokens.

**Authorization baseline**
- Every `AddAuthorizationFor*` sets `AuthorizationOptions.FallbackPolicy`, which applies only to
  endpoints with **no** authorization metadata. An endpoint with `[Authorize(Roles = ...)]` or a
  named policy replaces the baseline instead of adding to it.
- `AddAuthorizationForKeycloakAndRegularUserSchemes` requires the `Administrator` role, not
  `RegularUser` — despite its XML doc. Internal users are denied by default under it.
- `Administrator` is granted to every Keycloak-authenticated principal, by `MultiJwtClaimsTransformer`,
  and never by the library's own login. That transformer reads `IOptions<KeycloakConfiguration>`, so
  `services.Configure<KeycloakConfiguration>(...)` is needed in addition to passing the object to
  `AddTokenAuthentication`. Without it Keycloak users get no role and are denied.
- `SecurityPolicy.Metrics` is `RequireAssertion(_ => true)`: it lets **anyone** in, authenticated or
  not. Use it only on a Prometheus endpoint.
- `AddAuthorizationForRegularUserKeycloakAndApiCertificate` does not register `UserCreationPolicy`.
  An endpoint naming that policy throws at request time.

**Permission gate**
- The gate inherits the `FallbackPolicy` and only adds to it. Under a `RegularUser` baseline an
  Administrator is denied even by `[AdministratorOrUserWithPermission]`.
- `[UserWithPermission]` denies every Keycloak principal. With Keycloak, use
  `[AdministratorOrUserWithPermission]`.
- `[AllowAnonymous]`, including one inherited from a base controller, switches the gate off.
- Tags are compared ordinally and case-sensitively. A typo denies everyone without an error, so
  declare tags as `const string`.
- `Console` (certificate) and `ExternalSystem` principals are always denied on gated endpoints.
  Adding an attribute to an endpoint an integration calls breaks that integration.

**Management controllers**
- Two `BaseGroupManagementController` and two `BasePermissionManagementController` exist. The ones in
  `Cause.SecurityManagement.Controllers` (`api/groups`, `api/permissions`) are `[Obsolete]`. Use the
  ones in `Cause.SecurityManagement.Controllers.Management`.
- The default convention requires an authenticated caller, not a permission. Put a permission attribute
  on every management subclass. Implement the row-scope methods (`CanEditUserAsync`,
  `CanGrantGroupsAsync`, `CanViewPermissionsForUserAsync`) with a real check, never `true`.

**Runtime state**
- `SecurityManagementOptions` keeps the MFA flag, the current-password flag and the authentication
  scheme in **static** properties, and each `new SecurityManagementOptions()` resets both flags. Calling
  `InjectSecurityServices` twice in one process (test hosts, two registrations) keeps the last call's
  settings for everyone.

## Before production

Authentication and authorization changes protect 9-1-1 data. Before they reach production:
- write an integration test per role (anonymous, `RegularUser` with and without the permission,
  Keycloak `Administrator`, `Console`, `ExternalSystem`) that asserts 200/401/403 on each changed
  endpoint;
- list the console and external-system integrations that call a changed endpoint, and confirm each
  still authenticates;
- route the change through `security-reviewer`.
