# Authentication and authorization

Pick one authentication extension (who the caller is), then one authorization baseline (what an
undecorated endpoint requires). Add the permission gate on top when endpoints need permission tags:
see [permission-gate.md](permission-gate.md).

## Roles (`SecurityRoles`, namespace `Cause.SecurityManagement.Core`)

| Constant | Value | Granted to |
|---|---|---|
| `User` | `RegularUser` | Internal user after a complete login |
| `Administrator` | `Administrator` | **Every** Keycloak-authenticated principal (claims transformer). Never by the library's login |
| `ApiCertificate` | `Console` | A console integration. The library never issues this role: it routes a Bearer JWT carrying it to the host's console handler (`AddTokenAuthenticationWithCertificates<THandler>`) |
| `ExternalSystem` | `ExternalSystem` | External system, by certificate or by token |
| `UserLoginWithMultiFactor`, `UserRecovery`, `UserCreation`, `UserPasswordSetup` | same | Temporary roles on a short token (`TemporaryAccessTokenLifeTimeInMinutes`) during MFA, recovery, creation or password setup |

`SecurityRoles.UserAndUserCreation` and `UserAndUserRecovery` are comma-joined strings meant for
`[Authorize(Roles = ...)]`, which splits on commas.

## Authentication extensions

| Extension | Package | Schemes | Use for |
|---|---|---|---|
| `AddTokenAuthentication(securityConfiguration, keycloakConfiguration?)` | `Cause.SecurityManagement` | Internal JWT, plus Keycloak JWT when the second argument is not null. A policy scheme picks one per request from the token's issuer and roles | Most APIs |
| `AddTokenAuthenticationWithCertificates<THandler>(securityConfiguration, keycloakConfiguration?)` | `Cause.SecurityManagement` | Same, plus a console scheme (`ConsoleCertificateAuthentication`) handled by your `THandler : AuthenticationHandler<CertificateAuthenticationOptions>` | APIs also called by console integrations |
| `AddSimpleTokenAuthentication(securityConfiguration)` | `Cause.SecurityManagement` | Internal JWT only, no claims transformer | Internal-only APIs without Keycloak |
| `AddExternalSystemTokenAuthentication(securityConfiguration)` | `Cause.SecurityManagement` | External-system JWT only | APIs called only by external systems, without certificates |
| `AddDualExternalSystemAuthentication(securityConfiguration)` | `Cause.SecurityManagement` | External-system JWT when a `Bearer` header is present, otherwise client certificate | External systems that may use either |
| `AddExternalCertificateAuthentication(scheme = "CertificateAuthentication")` | `Core` | Client certificate, set as the default scheme | Certificate-only APIs |

Scheme names are constants of `CustomAuthSchemes` (namespace `Cause.SecurityManagement.Core.Authentication`).
Use the constants, never string literals.

Under `AddTokenAuthentication*`, the policy scheme sends a request to Keycloak when the token's `iss`
is the Keycloak `ValidIssuer`, to the internal scheme when it carries `RegularUser`, to the console
scheme when it carries `Console`, and to the internal scheme in every other case: no `Bearer` header,
an unreadable token, or unknown roles.

### Keycloak

Keycloak needs two registrations, because two components read it differently:

```csharp
var keycloakSection = configuration.GetSection(KeycloakConfiguration.Name);
services.Configure<KeycloakConfiguration>(keycloakSection);
services.AddTokenAuthentication(securityConfiguration, keycloakSection.Get<KeycloakConfiguration>());
```

- The argument configures the Keycloak JWT bearer and the scheme selection
  (`iss == ValidIssuer` → Keycloak).
- `IOptions<KeycloakConfiguration>` is read by `MultiJwtClaimsTransformer`, which adds the
  `Administrator` role, and by `GET api/KeycloakConfiguration`, which serves the web client its
  configuration anonymously.

A Keycloak principal carries `Administrator` and no `RegularUser`. Under a `RegularUser` baseline it is
denied everywhere unless an endpoint admits `Administrator` explicitly.

### Certificates — trust boundary

Certificate authentication does not see the TLS handshake. It reads headers set by the reverse proxy
that terminates TLS: `ssl-client-verify` (must be `SUCCESS`), `ssl-client-subject-dn` (looked up as
the external system's certificate subject) and `ssl-client-issuer-dn` (checked against
`CertificateIssuers`).

- The API must be reachable **only** through that proxy, and the proxy must overwrite these headers
  on every request. A caller that reaches the API directly can forge them.
- An empty or missing `CertificateIssuers` skips the issuer check.
- A subject DN matching no external system gives a 401. A DN matching **more than one** throws
  `DuplicateCertificateSubjectException`, logged at Error: the caller gets a 500, which signals a data
  problem to fix, not a bad certificate (ADR `2026-08-13-certificate-authentication-failure-semantics`).

## Authorization baselines (`AddAuthorizationFor*`, package `Core`)

Each one sets `AuthorizationOptions.FallbackPolicy`, adds named policies, and by default registers
`AddAuthorizationRejectionLogging()` (pass `logAuthorizationRejections: false` to opt out).

| Extension | Fallback requires | Fallback schemes | Named policies added |
|---|---|---|---|
| `AddAuthorizationForRegularUser` | `RegularUser` | default scheme | UserRecovery, UserCreation, UserPasswordSetup, Metrics |
| `AddAuthorizationForRegularUserAndExternalSystem` | `RegularUser` | default scheme | same + ExternalSystem |
| `AddAuthorizationForRegularUserKeycloakAndApiCertificate` | `RegularUser` or `Console` or `Administrator` | Keycloak, internal, console | ApiCertificate, UserRecovery, UserPasswordSetup, Metrics (**no** UserCreation) |
| `AddAuthorizationForKeycloakAndRegularUserSchemes` | **`Administrator` only** | Keycloak, internal | Metrics |
| `AddAuthorizationForExternalSystem` | `ExternalSystem` | default scheme | ExternalSystem |

Policy names are constants of `SecurityPolicy`: `UserRecoveryPolicy`, `UserCreationPolicy`,
`UserPasswordSetupPolicy`, `ApiCertificatePolicy`, `MetricsPolicy`, `ExternalSystemPolicy`.
`SecurityPolicy` also declares `Keycloak`, `RegularUser` and `KeycloakAndRegularUser`, which no
extension registers: naming one in `[Authorize(Policy = ...)]` throws at request time.

- `MetricsPolicy` is `RequireAssertion(_ => true)`: it admits anonymous callers. Use it only for a
  metrics scrape endpoint.
- `UserRecoveryPolicy` and `UserCreationPolicy` pass the comma-joined constants
  (`SecurityRoles.UserAndUserRecovery`, `UserAndUserCreation`) to `RequireRole`, which does not split
  on commas, so in 11.0.0 they admit no one. On your own endpoints use
  `[Authorize(Roles = SecurityRoles.UserAndUserRecovery)]`, as the library's controllers do.
- The fallback applies only to endpoints with no authorization metadata. `[Authorize(Roles = ...)]`
  or `[Authorize(Policy = ...)]` on an endpoint replaces it.
- `customer-portal` and `mass-alert` build their `FallbackPolicy` by hand with `AddAuthorizationCore`.
  That is valid; keep the authentication schemes on the `FallbackPolicy` (not only on
  `DefaultPolicy`), because the permission gate inherits only the fallback.

### Endpoint attributes (`Core`)

`[AuthorizeForUserRolesAttributes]` (RegularUser), `[AuthorizeForAdministratorRole]`,
`[AuthorizeForCertificateRole]` (Console), `[AuthorizeForUserAndAdministratorRoles]`,
`[AuthorizeForUserAdministratorAndCertificateRoles]` (RegularUser, Administrator, Console,
ExternalSystem). Each replaces the fallback on the endpoint it decorates.

## MVC conventions (package `Cause.SecurityManagement`)

For new APIs, rely on the `FallbackPolicy` of an `AddAuthorizationFor*` baseline. The two older MVC
mechanisms below add `AuthorizeFilter`s. Under endpoint routing (the default), MVC does not turn
`[Authorize]` attributes into filters, and `AuthorizeFilter` combines (AND) its own policy with the
endpoint's authorization metadata. A global or controller-level filter therefore tightens every
endpoint instead of yielding to the endpoint's attribute. This is read from the ASP.NET Core 10
source, not tested: confirm with an integration test before changing an application that uses them.

- `MvcOptionsExtensions.AskForAuthorizationByDefault`: a global `RegularUser` filter. Combined with
  `[Authorize(Roles = ...)]` endpoints, it refuses Keycloak Administrators and tokens holding only a
  temporary role (MFA, recovery, password setup).
- `AddAuthorizeFiltersControllerConvention` with `AddBasicPoliciesForCertificateLogon()`: per
  controller, `[OpenToExternalSystem]` → `apipolicy` (ExternalSystem), `[OpenToExternalSystemWithCertificate]`
  → `apicertificatepolicy` plus `MultipleSchemeRequireUserAttribute`, otherwise → `defaultpolicy`
  (RegularUser) unless the controller has `[Authorize]`.
  - `apicertificatepolicy` names two schemes: `CertificateAuthenticationOptions.Name`
    (`"CertificateScheme"`), which no library extension registers (`AddExternalCertificateAuthentication`
    defaults to `"CertificateAuthentication"`), and `SecurityManagementOptions.AuthenticationScheme`
    (`"Bearer"` by default), which only `AddSimpleTokenAuthentication` registers. Register handlers under
    both names, or define your own policies as `ali-voip` does.
  - `MultipleSchemeRequireUserAttribute` is an action filter that returns 401 without a `sid` claim and
    ignores `[AllowAnonymous]`.

`[AllowAnonymous]` skips `AuthorizeFilter` authorization, but not `MultipleSchemeRequireUserAttribute`.

## Rejection logging

`AddAuthorizationRejectionLogging()` logs one Information line per 401, naming the schemes consulted
and why each failed (category `Cause.SecurityManagement.Core.Authentication.AuthorizationRejectionLogger`).
It replaces any `IAuthorizationMiddlewareResultHandler` the application registered (a startup warning
names the displaced type). Never set `IdentityModelEventSource.ShowPII = true` outside a debugging
session: the logged reasons would then contain token material.
