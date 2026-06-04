# Cause.SecurityManagement

ASP.NET Core HTTP integration for the **Cause.SecurityManagement** platform:
the MVC controllers (authentication, user management, and group & permission
management) layered on top of `Cause.SecurityManagement.Core`.

The controllers ship as **abstract base controllers** that the host application
subclasses to activate. They are registered through `InjectSecurityControllers()`
and pick up the project's default authorization.

## Installation

```
dotnet add package Cause.SecurityManagement
```

This package depends on `Cause.SecurityManagement.Core` (and transitively on
`Cause.SecurityManagement.Models`).

## Quick start

```csharp
// Activate a controller by subclassing the abstract base:
using Cause.SecurityManagement.Controllers.Management;

public class GroupManagementController(
    IGroupManagementApiService service,
    IValidator<GroupDto> validator)
    : BaseGroupManagementController(service, validator);

// Register the controllers:
services.AddMvc(...).InjectSecurityControllers();
```

See the repository README for the full setup guide (configuration, models,
`DbContext`, authentication, and the group & permission management API).

## Coordinated versioning

All `Cause.SecurityManagement.*` packages share a single version and are
released together. Pin every `Cause.SecurityManagement.*` reference in your
application to the same version — mixing versions from different release sets is
unsupported. See the repository's `docs/RELEASING.md` for details.

## Related packages

| Package | Responsibility |
|---|---|
| `Cause.SecurityManagement.Models` | Base models and contracts |
| `Cause.SecurityManagement.Core` | Core security services |
| `Cause.SecurityManagement` | ASP.NET Core HTTP/MVC integration (this package) |
