# Cause.SecurityManagement.Core

Core security services for ASP.NET Core: JWT authentication, authorization,
user management, group & permission management, and the supporting
repositories and validators.

This is the heart of the **Cause.SecurityManagement** platform. It provides the
service registration entry point (`InjectSecurityServices<TUser>()`), the
authentication schemes (internal user, Keycloak, and external certificate), the
authorization policies, and the asynchronous, cancellation-aware group &
permission management services. It depends only on
`Cause.SecurityManagement.Models`.

## Installation

```
dotnet add package Cause.SecurityManagement.Core
```

## Quick start

```csharp
services.Configure<SecurityConfiguration>(Configuration.GetSection("APIConfig"));
services.InjectSecurityServices<User>(options =>
{
    options.UseMultiFactorAuthentication();
    options.SetValidationCodeSender<MySmsSender>();
});

var securityConfiguration = Configuration
    .GetSection(nameof(SecurityConfiguration))
    .Get<SecurityConfiguration>();
services.AddTokenAuthentication(securityConfiguration);
```

For HTTP controllers (authentication, user, group and permission management),
add the `Cause.SecurityManagement` package on top of this one.

## Coordinated versioning

All `Cause.SecurityManagement.*` packages share a single version and are
released together. Pin every `Cause.SecurityManagement.*` reference in your
application to the same version — mixing versions from different release sets is
unsupported. See the repository's `docs/RELEASING.md` for details.

## Related packages

| Package | Responsibility |
|---|---|
| `Cause.SecurityManagement.Models` | Base models and contracts |
| `Cause.SecurityManagement.Core` | Core security services (this package) |
| `Cause.SecurityManagement` | ASP.NET Core HTTP/MVC integration (controllers) |
