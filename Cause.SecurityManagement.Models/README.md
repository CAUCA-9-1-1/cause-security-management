# Cause.SecurityManagement.Models

Base models and data contracts for the **Cause.SecurityManagement** security platform.

This package contains the plain domain entities and DTOs shared across the
platform (users, groups, module permissions, and the management API contracts
such as `GroupListItem`). It carries no ASP.NET or message-bus dependencies, so
it can be referenced from any layer that only needs the data shapes.

## Installation

```
dotnet add package Cause.SecurityManagement.Models
```

## Coordinated versioning

All `Cause.SecurityManagement.*` packages share a single version and are
released together. Pin every `Cause.SecurityManagement.*` reference in your
application to the same version — mixing versions from different release sets is
unsupported. See the repository's `docs/RELEASING.md` for details.

## Related packages

| Package | Responsibility |
|---|---|
| `Cause.SecurityManagement.Models` | Base models and contracts (this package) |
| `Cause.SecurityManagement.Core` | Core security services (authentication, authorization, user & group management) |
| `Cause.SecurityManagement` | ASP.NET Core HTTP/MVC integration (controllers) |
