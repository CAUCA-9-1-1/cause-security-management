# Wolverine hosting

## What is published

| Package | Status | Content |
|---|---|---|
| `Cause.SecurityManagement.Wolverine.ExternalSystem` | Published (11.0.0) | `POST /api/ExternalSystemAuthentication/Logon` and `/Refresh`, their legacy aliases `/api/Authentication/LogonForExternalSystem` and `/RefreshForExternalSystem`, and `GET /api/Me` |
| `Cause.SecurityManagement.Wolverine` | **Not published** (`IsPackable=false`, `10.2.0-experimental5`; ADR `2026-06-04-exclude-wolverine-from-published-release-set`) | User authentication endpoints, group and permission management endpoints, login and recovery sagas |

The repository README still shows `dotnet add package Cause.SecurityManagement.Wolverine`. That
package cannot be restored from the feed. A Wolverine API that needs internal-user login today keeps
the MVC `AuthenticationController` (`Cause.SecurityManagement` + `InjectSecurityControllers()`)
alongside its Wolverine endpoints, or waits for the package to be published. Ask before planning
around the unpublished package.

## External-system endpoints

```csharp
builder.Services.Configure<SecurityConfiguration>(builder.Configuration.GetSection("APIConfig"));
builder.Services.InjectSecurityServices<User>();
builder.Services.AddExternalSystemTokenAuthentication(securityConfiguration); // or AddDualExternalSystemAuthentication
builder.Services.AddAuthorizationForExternalSystem();

builder.Host.UseWolverine(opts =>
{
    opts.AddExternalSystemEndpoints();
});

app.UseAuthentication();
app.UseAuthorization();
app.MapWolverineEndpoints();
```

`AddExternalSystemEndpoints()` (namespace `Cause.SecurityManagement.Wolverine.ExternalSystem`) adds
the endpoint assemblies to Wolverine discovery. It needs `InjectSecurityServices` for
`IExternalSystemAuthenticationService` and the repositories.

The package depends only on `Core` and `WolverineFx.Http`. `AddExternalSystemTokenAuthentication`
and `AddDualExternalSystemAuthentication` live in the `Cause.SecurityManagement` (MVC) package, so a
Wolverine-only API references it for token authentication; certificate-only APIs can use
`AddExternalCertificateAuthentication` from `Core`. For Wolverine conventions
(handlers, codegen, `TypeLoadMode`), use the `wolverine-dotnet` skill.

## Full package (unpublished) — for reference

When `Cause.SecurityManagement.Wolverine` is published, `opts.AddSecurityManagementHandlers()` adds its
whole assembly to discovery and calls `AddExternalSystemEndpoints()`. Its routes mirror the MVC
controllers (`/api/Authentication/*`, `/api/KeycloakConfiguration`, `/GroupManagement`, `/UserSearch`,
`/PermissionManagement`), so existing clients keep working. It has no equivalent of
`BaseUserEditionController`, `BaseUserPermissionDetailsController` or the OData list controllers. The
OData group feed stays the host's responsibility, as with MVC.

Sagas:
- `UserLoginSaga`, correlated on the user id: `StartUserLogin(userId)` after a login returning
  `MustVerifyCode = true`, then `ResendLoginCode(userId, communicationType)` or
  `VerifyLoginCode(userId, code)` → `LoginResult`.
- `AccountRecoverySaga`, correlated on the trimmed user name or email:
  `StartAccountRecovery(usernameOrEmail)`, then `ValidateAccountRecovery(usernameOrEmail, code)` →
  `LoginResult` with `MustChangePassword = true`.
