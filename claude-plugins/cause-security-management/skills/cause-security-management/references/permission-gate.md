# Permission gate

Restricts an endpoint to callers holding a permission tag (a `ModulePermission` feature name stored in
the security tables). ADR `2026-08-11-permission-based-authorization-gate`.

## Registration

```csharp
services.AddAuthorizationForRegularUser();   // or another baseline, or a hand-built FallbackPolicy
services.AddPermissionBasedAuthorization();  // order does not matter
```

`AddPermissionBasedAuthorization(validateTagsAtStartup: false)` replaces `IAuthorizationPolicyProvider`
and adds `AddAuthorization()` (so `UseAuthorization()` works in a minimal API).
`validateTagsAtStartup: true` logs one warning per distinct attribute tag missing from the permission
catalog. It never fails startup; when the catalog cannot be read, it logs a warning and skips the
check.

## Usage

```csharp
public static class Permissions
{
    public const string CanEditBuilding = "CanEditBuilding";
}

[AdministratorOrUserWithPermission(Permissions.CanEditBuilding)]
public async Task<IActionResult> EditBuilding(...) { }

app.MapPut("/buildings/{id}", EditBuilding)
   .RequireAuthorization(PermissionPolicy.NameFor(Permissions.CanEditBuilding, allowAdministrator: true));
```

Attributes are in `Cause.SecurityManagement.Core`; `PermissionPolicy` in
`Cause.SecurityManagement.Core.Authentication`.

| Attribute | Administrator (Keycloak) | RegularUser | Console, ExternalSystem, others |
|---|---|---|---|
| `[AdministratorOrUserWithPermission(tag)]` | passes without a database read | passes only with the tag | 403 |
| `[UserWithPermission(tag)]` | 403 | passes only with the tag | 403 |

**Which attribute:** with Keycloak, `[AdministratorOrUserWithPermission]`. Without Keycloak there are
no Administrator principals, so `[UserWithPermission]` is equivalent and states the intent.
`[UserWithPermission]` is not the "general" form: it denies every Keycloak principal.

## How the policy is built

The generated policy is `FallbackPolicy` + `PermissionRequirement`. A decorated endpoint is therefore
always at least as strict as an undecorated one.

- Under a `RegularUser` fallback (`AddAuthorizationForRegularUser`, or the hand-built fallback of
  `customer-portal` and `mass-alert`), an Administrator is denied **even by**
  `[AdministratorOrUserWithPermission]`. Administrators pass only where the fallback admits them, as
  in `AddAuthorizationForRegularUserKeycloakAndApiCertificate`.
- The fallback's authentication schemes are inherited; schemes set only on `DefaultPolicy` are not.
- Useless combinations: `AddAuthorizationForKeycloakAndRegularUserSchemes` (fallback admits only
  Administrator: `[AdministratorOrUserWithPermission]` is a no-op, `[UserWithPermission]` denies
  everyone) and `AddAuthorizationForExternalSystem` (both attributes deny everyone).

## Behaviours

- **AND, never OR.** Two attributes on one action, or one on the controller and one on the action,
  require both tags. Mixing the two attribute types denies Administrators. For "tag A or tag B", write
  a custom `IAuthorizationRequirement` (as `customer-portal` does for its intervention-file policies).
- **`[AllowAnonymous]` switches the gate off**, including when inherited from a base controller.
- **Tags compare ordinally, case-sensitively.** `"caneditbuilding"` does not match `CanEditBuilding`
  and silently denies everyone. Use constants.
- **A user's tag must be allowed.** The check reads the user's merged permissions (groups and direct
  grants); a tag the user does not hold, or holds as denied, fails.
- **`Console` and `ExternalSystem` principals always fail.** Before adding an attribute to an existing
  endpoint, find the integrations that call it.
- **Cost:** one permission read per request per user, memoised for the request, none for
  Administrators. No cross-request cache: a revoked permission applies on the next request.

## Diagnosing an unexpected 403

1. Which principal? `GET api/Me` shows its roles. Keycloak → `Administrator`; internal → `RegularUser`.
2. Does the fallback admit that role? See the baseline table in
   [authentication-and-authorization.md](authentication-and-authorization.md).
3. Which attribute? `[UserWithPermission]` + Keycloak = always 403.
4. Is the tag spelled exactly as stored, case included? Turn on `validateTagsAtStartup` to list tags
   missing from the catalog.
5. Does the user hold the tag as allowed, through a group or directly? `GET UserPermissionDetails/user/{userId}`
   (when the host exposes it) shows the effective state and its sources.

A 401 instead of 403 means authentication failed: read the line logged by `AuthorizationRejectionLogger`.

## Testing

Cover every gated endpoint with integration tests asserting the status for: anonymous (401), a
RegularUser without the tag (403), a RegularUser with it (200), a RegularUser holding it as denied
(403), a Keycloak Administrator (per the table above), and any Console or ExternalSystem caller that
used the endpoint before (403 — confirm this is intended).
