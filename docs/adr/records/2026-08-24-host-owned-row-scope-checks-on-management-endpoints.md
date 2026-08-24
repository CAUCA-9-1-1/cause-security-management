# Host-Owned Row-Scope Checks On Management Endpoints

* Status: accepted
* Date: 2026-08-24
* Deciders: Cause.SecurityManagement maintainers
* Technical Story: Issue #117

## Context and Problem Statement

The library exposes management endpoints that consuming applications activate by
subclassing an abstract controller — `BaseGroupListController`,
`BasePermissionManagementController`, `BaseUserSearchController`. This record adds
two more: `BaseUserListController<TUserListItem>` for the users OData feed, and
`BaseUserPermissionDetailsController`, which returns one user's effective
permissions for a management screen.

Both raise a question the existing endpoints did not. They are addressed at a
specific user, and consuming applications restrict which users a caller may see on
rules the library does not model: customer-portal scopes by fire safety department,
mass-alert by city, SURVI-Mobile by department group. None of that data exists in
the security schema.

The permission gate recorded in
[2026-08-11-permission-based-authorization-gate.md](2026-08-11-permission-based-authorization-gate.md)
answers a different question. `[UserWithPermission("CanAccessUsers")]` states that a
caller may reach the screen at all. It cannot state that *this* caller may see
*that* user, because a policy sees the principal and the endpoint, not the row.

The risk is concrete. A list endpoint filters the rows a caller may see, so the
caller never sees a user outside their scope in the grid. A detail endpoint takes
the identifier directly. If it does not repeat the scope check, any holder of the
screen-level permission reads any user's permissions by guessing or replaying an
identifier — and reads them through an endpoint whose whole purpose is to disclose
authorization data.

## Decision Drivers

* Fail closed, consistent with the permission gate's own invariant.
* A scope rule the library cannot express must not be silently skipped.
* Do not require host applications to remember an unenforced convention.
* Keep the addition additive: no existing consumer may break.

## Considered Options

* **Option A**: Declare the check `abstract` on the base controller. The endpoint
  does not compile into a host application until that host has written the rule.
* **Option B**: Declare it `virtual` returning `true`. Hosts needing a scope rule
  override it.
* **Option C**: Leave the check out of the library entirely and document that hosts
  must override the action itself to add one.
* **Option D**: Add a library abstraction for user scope — an
  `IUserScopeReader` the host implements and the library calls.

## Decision Outcome

Chosen option: **Option A**, because it is the only option where forgetting the
rule is a build failure rather than a data leak.

Option B was rejected because its failure mode is invisible. A host that never
hears about the method ships an endpoint disclosing every user's permissions, and
nothing errors, nothing logs, and no test fails — the endpoint works exactly as
written. The cost of Option A is one method per host; the cost of Option B is
discovered in production or not at all.

Option C was rejected because overriding an action means restating its route,
attributes and Swagger documentation, which is precisely the duplication these base
controllers exist to remove. It also invites a host to override and drop the
`[ProducesResponseType]` metadata silently.

Option D was rejected as speculative generality. Three consuming applications scope
users three different ways, and none of the three has a shape the library can name
today. A `Task<bool>` on the controller is the whole contract that is actually
shared; the host is free to back it with a reader of its own, and customer-portal
does exactly that with `IUserVisibilityReader`.

### Where The Two Gates Meet

The two mechanisms are complementary and both are expected on a user-addressed
management endpoint:

| Question | Mechanism | Owner |
|---|---|---|
| May this caller reach this screen? | `[UserWithPermission(tag)]` / `[AdministratorOrUserWithPermission(tag)]` | the host, declaratively on its subclass |
| May this caller see this user? | `CanViewPermissionsForUserAsync` | the host, as code |

`BaseUserListController<TUserListItem>` carries no such method on purpose: its
`Get()` returns an `IQueryable` the host builds, so the host applies its scope
filter inside the projection it already owns. Adding a second mechanism there would
give two places to express one rule.

### Consequences

* Good: An unimplemented scope rule cannot ship. It is a compile error.
* Good: The rule lives in the host, where the data it needs lives, with no library
  abstraction invented ahead of a real second shape.
* Good: `403` rather than `404` on refusal, so a caller cannot use the status code
  to probe which user identifiers exist.
* Bad: Every host subclassing `BaseUserPermissionDetailsController` writes a
  method, including a host that genuinely has no scope rule and returns `true`.
  That explicit `true` is the point: it is a decision on the record.
* Bad: The asymmetry between the two new controllers — one with an abstract check,
  one without — has to be explained, which is why it is written down here.

## Implementation Plan
<!-- Crucial section so Claude Code knows how to execute it -->
- [x] Task 1: Add `UserListItem` to `Cause.SecurityManagement.Models`, and
      `BaseUserListController<TUserListItem>` to
      `Cause.SecurityManagement/Controllers/Management/`.
- [x] Task 2: Add `PermissionStatus`, `UserPermissionOriginKind`,
      `UserPermissionOriginDto` and `UserPermissionDetailDto` under
      `Models/DataTransferObjects/Management/`.
- [x] Task 3: Add `IUserPermissionDetailReader` and
      `UserPermissionDetailReader<TUser>` under
      `Cause.SecurityManagement.Core/Services/Management/`, computing status with
      the same "one denial wins" rule as `PermissionMergeTool`. Register it in
      `AddBaseConfiguration`.
- [x] Task 4: Add `BaseUserPermissionDetailsController` with the abstract
      `CanViewPermissionsForUserAsync` and a `403` on refusal.
- [x] Task 5: Unit tests — the three permission statuses, the "one denial wins"
      rule including a user override that allows, origin reporting, catalog
      ordering, another user's assignments ignored, and the controller returning
      `403` without reading any permission.
- [x] Task 6: Build with no new warnings, run the full unit suite, bump the four
      packable projects in lockstep for the `release.ps1` version gate.
