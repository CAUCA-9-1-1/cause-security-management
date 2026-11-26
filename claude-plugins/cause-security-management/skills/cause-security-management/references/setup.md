# Setup

How to wire Cause.SecurityManagement into an ASP.NET Core API that uses MVC controllers. For
Wolverine HTTP, read this file for the configuration and the DbContext, then [wolverine.md](wolverine.md).

## Configuration

`SecurityConfiguration` is bound from the `APIConfig` section. Every Cauca API uses that name.

```json
"APIConfig": {
  "Issuer": "https://my-api.example.com/",
  "PackageName": "My-Application-Name",
  "SecretKey": "<from the secret store, never committed>",
  "CertificateIssuers": [],
  "MinimalVersion": "1.0.0"
},
"KeycloakConfiguration": {
  "Url": "https://keycloak.example.com",
  "Realm": "my-realm",
  "ClientId": "my-client",
  "Resource": "my-client",
  "MetadataAddress": "https://keycloak.example.com/realms/my-realm/.well-known/openid-configuration",
  "ValidIssuer": "https://keycloak.example.com/realms/my-realm"
}
```

`SecurityConfiguration` (namespace `Cause.SecurityManagement.Models.Configuration`):

| Property | Default | Role |
|---|---|---|
| `Issuer`, `PackageName`, `SecretKey` | — | Issuer, audience and signing key of the library's own JWT |
| `PreviousSecretKey`, `AllowTokenRefreshWithPreviousSecretKey` | `null`, `false` | Key rotation: accept a refresh signed with the old key |
| `AccessTokenLifeTimeInMinutes` | 60 | Access token lifetime |
| `RefreshTokenLifeTimeInMinutes` | 540 | Refresh token lifetime |
| `TemporaryAccessTokenLifeTimeInMinutes` | 5 | Token with a temporary role (MFA, recovery, password setup) |
| `RefreshTokenCanExpire` | `true` | `false` makes refresh tokens permanent |
| `RequiredPermissionForLogin` | `null` | Permission tag a user needs to log in |
| `RequiredPermissionForAllGroupsAccess` | `null` | Legacy services only (`api/groups`, `api/users`): a user without this tag reaches only groups marked `AssignableByAllUsers`. The new management controllers ignore it; enforce the rule in `CanGrantGroupsAsync` |
| `CertificateIssuers` | — | Accepted suffixes of the `ssl-client-issuer-dn` header for certificate authentication. **Empty or missing accepts any issuer** |
| `MinimalVersion`, `LatestVersion` | — | Mobile client version checks (`VersionValidator` endpoints), semantic versions |

`KeycloakConfiguration`: `Url`, `Realm`, `ClientId`, `Resource` (also the audience),
`MetadataAddress`, `ValidIssuer`, `AuthorizationUrl`, `SslRequired` (default `true`),
`ValidateSigningKey` (default `true`), `ShowDebugInfo` (default `false`; when `true`, a failed Keycloak
token returns diagnostic JSON and logs every claim: never enable it in production).

`SecretKey` is a signing secret. Keep it in the deployment secret store, not in a committed
`appsettings.json`.

## Model and DbContext

- The user entity inherits `Cause.SecurityManagement.Models.User` and adds what the application needs.
- The context inherits `BaseSecurityContext<TUser>` (namespace `Cause.SecurityManagement.Core`).
  Override `OnModelCreating` and call the protected `AddSecurityManagementMappings(modelBuilder)`.
  The base `OnModelCreating` calls `UseAutoDetectedMappings` (Cause.Core.DataLayerExtensions), which
  scans only the **application's** assembly: the library's mappings, which live in
  `Cause.SecurityManagement.Core`, are not applied by it.

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    AddSecurityManagementMappings(modelBuilder);
    this.UseAutoDetectedMappings(modelBuilder);
}
```
- To customise the user mapping, inherit `UserMapping<TUser>` (namespace
  `Cause.SecurityManagement.Core.Mapping`) and override `MapProperties`.
- The library resolves its data through `ISecurityContext<TUser>`. Register it, pointing at the
  application's context:

```csharp
services.AddScoped<ISecurityContext<User>>(provider => provider.GetRequiredService<MyDbContext>());
```

The security tables come through EF Core migrations of the application's own context. Review a
generated migration that touches them like any other data-loss risk.

## Registration (MVC)

```csharp
var securityConfiguration = configuration.GetSection("APIConfig").Get<SecurityConfiguration>();

services.Configure<SecurityConfiguration>(configuration.GetSection("APIConfig"));
services.InjectSecurityServices<User>(options =>
{
    options.UseMultiFactorAuthentication<ValidationCodeSender, ValidationCodeValidator>();
    options.SendEmailWhenUserAreBeingModified<EmailForUserModificationSender>();
});
services.AddTokenAuthentication(securityConfiguration);
services.AddAuthorizationForRegularUser();
services.AddPermissionBasedAuthorization();

services.AddControllers()
    .InjectSecurityControllers();

// pipeline
app.UseAuthentication();
app.UseAuthorization();
```

- `services.Configure<SecurityConfiguration>` is required: services and the claims transformer read
  `IOptions<SecurityConfiguration>`. The object passed to `AddTokenAuthentication` only configures the
  JWT bearer.
- With Keycloak, also call `services.Configure<KeycloakConfiguration>(configuration.GetSection(KeycloakConfiguration.Name))`
  and pass the bound object as the second argument of `AddTokenAuthentication`. See
  [authentication-and-authorization.md](authentication-and-authorization.md).
- The `AddAuthorizationFor*` call sets the `FallbackPolicy`, which is what requires authentication on
  every endpoint without authorization metadata. That is the secure default to rely on.
- `InjectSecurityControllers()` adds the library assembly as an MVC application part. It activates the
  concrete controllers `AuthenticationController`, `ExternalSystemAuthenticationController`,
  `KeycloakConfigurationController` and `MeController`. `ExternalSystemAuthenticationController` adds
  **anonymous** external-system logon and refresh endpoints (and the legacy aliases
  `/api/Authentication/LogonForExternalSystem` and `/RefreshForExternalSystem`) to every MVC host,
  whether or not it has external systems. The abstract `Base*Controller` classes are only activated by
  a subclass in the application.
- Do not add `MvcOptionsExtensions.AskForAuthorizationByDefault` to a new API. Under endpoint routing,
  MVC does not turn `[Authorize]` attributes into filters, so this global `RegularUser` filter always
  runs and is combined (AND) with each endpoint's own requirement: a Keycloak Administrator, or a
  token carrying only a temporary role (the MFA `validationCode` and `PasswordSetup` endpoints), is
  refused. This comes from reading the ASP.NET Core 10 source, not from a test; existing applications
  that use it (`commprovider`, `gestiondaa`, with their own variant of the filter) should be checked by an integration
  test before they are changed.

## `InjectSecurityServices<TUser>` options

`TUser : User, new()`. Every option is optional.

| Option | Effect |
|---|---|
| `UseMultiFactorAuthentication()` | Turns on MFA; you register the sender and validator yourself |
| `UseMultiFactorAuthentication<TSender>()` | MFA with a code sender (`IAuthenticationValidationCodeSender<TUser>`); the library checks the stored code itself |
| `UseMultiFactorAuthentication<TSender, TValidator>()` | MFA with a sender and a validator (`IAuthenticationValidationCodeValidator<TUser>`) |
| `UseCurrentPasswordValidationOnPasswordChange()` | A password change requires the current password |
| `UseCustomAuthenticationScheme(scheme)` | Changes the `AuthorizationType` returned in `LoginResult` (default `Bearer`) and the second scheme of `apicertificatepolicy`. It changes no `AddAuthorizationFor*` policy |
| `SendEmailWhenUserAreBeingModified<T>()` | `IEmailForUserModificationSender` |
| `ManageDeviceWhenCreatingRegularUser<T>()` | `IDeviceManager` |
| `SetUserAdditionalInformationProvider<TUser, T>()` | Adds application claims to the token (`IUserAdditionalInformationProvider<TUser>`) |
| `SetCustomUserManagementService<TUser, TService, TImpl>()` | Replaces `IUserManagementService<TUser>` |
| `SetUserAuthenticationService<TService, TImpl>()` | Replaces `IUserAuthenticator` |
| `SetUserTokenRefresher<TService, TImpl>()` | Replaces `IUserTokenRefresher` |
| `SetCurrentUserService<TService, TImpl>()` | Replaces `ICurrentUserService` |

The MFA, current-password and scheme settings are **static** on `SecurityManagementOptions`. Each
`new SecurityManagementOptions()` (so each `InjectSecurityServices` call) resets the MFA and
current-password flags. Call `InjectSecurityServices` once per process.

## Administrator user

`IAdministratorUserGenerator.EnsureAdminIsCreated()` creates the internal user `admin` when it is
missing, with a **password hardcoded in the library** and therefore known to anyone who reads its
source. Use it only for a local or test database, or change the password (or deactivate the account)
right after the first start, before the API is reachable from a network. Resolve it from a scope after
the application is built, never through `services.BuildServiceProvider()` inside `ConfigureServices`
(that builds a second container).

```csharp
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<IAdministratorUserGenerator>().EnsureAdminIsCreated();
```

## Custom authentication source

To authenticate another kind of entity, subclass `BaseAuthenticationController` with your own
`IEntityAuthenticator` and `IEntityTokenRefresher`:

```csharp
[Route("api/[controller]"), ApiController]
public class DeviceAuthenticationController(
    IDeviceAuthenticator authenticator,
    IDeviceTokenRefresher tokenRefresher,
    ILogger<AuthenticationController> logger)
    : BaseAuthenticationController(authenticator, tokenRefresher, logger);
```

It inherits `Logon`, `Refresh`, `State`, `RecoverAccount`, `RecoverAccountValidation`,
`PasswordSetup` and the MFA `validationCode` routes.
