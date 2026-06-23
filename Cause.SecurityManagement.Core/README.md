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

## Populating `AdditionalInformation` on user DTOs

The user DTOs returned by the user search and group detail endpoints
(`UserForGroupDto` and `GroupUserDto`) expose an `AdditionalInformation` string.
By default it is `null`. To populate it, implement
`IUserAdditionalInformationProvider<TUser>` and return an expression that is
composed into the underlying EF Core query, so the value is computed in SQL in
the same round-trip as the rest of the projection:

```csharp
public class MyAdditionalInformationProvider : IUserAdditionalInformationProvider<User>
{
    public Expression<Func<User, string>> GetAdditionalInformation()
        => user => user.Email;
}
```

Register it through the options when calling `InjectSecurityServices`:

```csharp
services.InjectSecurityServices<User>(options =>
{
    options.SetUserAdditionalInformationProvider<User, MyAdditionalInformationProvider>();
});
```

The expression must be translatable to SQL by EF Core (reference `TUser`
properties or related columns; avoid client-only method calls). If no provider
is registered, `AdditionalInformation` is `null`.

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
