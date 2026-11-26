# Management controllers

Backend of the `@cauca-911/cauca-management` Angular library (users, groups, permissions). The
library ships **abstract** controllers in `Cause.SecurityManagement.Controllers.Management`; the host
activates each one by subclassing it. Routes and Swagger metadata are inherited. Every action is async
and passes the request `CancellationToken` down to EF Core.

`InjectSecurityServices<TUser>()` registers the services they need: `IGroupManagementApiService`,
`IUserSearchService`, `IPermissionCatalogService`, `IUserPermissionDetailReader`,
`IUserNameAvailabilityReader`, `IValidator<GroupDto>`.

## Two gates, both the host's job

1. **Who may open the screen** — a permission attribute on the subclass
   (`[UserWithPermission]` or `[AdministratorOrUserWithPermission]`, see
   [permission-gate.md](permission-gate.md)). The library's default convention only requires *an*
   authenticated caller; without an attribute, every authenticated user reaches the endpoint.
2. **Which rows this caller may act on** — the abstract `Can…Async` methods. They are abstract on
   purpose (ADR `2026-08-24-host-owned-row-scope-checks-on-management-endpoints`): forgetting them is a
   build error, not a data leak. Implement them with a real scope check. Returning `true` restores the
   leak the ADR closed; if every caller truly may act on every row, say why in the method name or a
   one-line comment.

`customer-portal` is the reference: it registers its own `IUserVisibilityReader` for these checks.

`SecurityConfiguration.RequiredPermissionForAllGroupsAccess` is enforced only by the legacy services.
If the application uses it, apply the same rule in `CanGrantGroupsAsync`.

## Routes have no `api/` prefix

The base routes are `GroupManagement`, `UserSearch`, `PermissionManagement`, `UserEdition`,
`UserPermissionDetails`. To serve them under `api/`, redeclare the route on each subclass
(`[Route("api/[controller]")]`) and keep the subclass named as the base expects
(`GroupManagementController`, …); otherwise the Angular client gets 404.

## Controllers

| Base class | Route | Host must provide |
|---|---|---|
| `BaseGroupManagementController(IGroupManagementApiService, IValidator<GroupDto>)` | `GroupManagement`: `GET name-availability`, `GET {groupId}`, `POST` (upsert), `DELETE {groupId}` | An empty subclass |
| `BaseUserSearchController(IUserSearchService)` | `UserSearch`: paged search over active users | An empty subclass |
| `BasePermissionManagementController(IPermissionCatalogService)` | `PermissionManagement`: assignable permission catalog | An empty subclass |
| `BaseUserEditionController<TUser, TUserForEdition>(IUserNameAvailabilityReader, IValidator<TUserForEdition>)` | `UserEdition`: `GET {userId}`, `POST` (create when `Id` is empty, update otherwise), `GET username-available` | `CanEditUserAsync`, `CanGrantGroupsAsync`, `ReadForEditionAsync`, `WriteForEditionAsync`, and the validator |
| `BaseUserPermissionDetailsController(IUserPermissionDetailReader)` | `UserPermissionDetails`: `GET user/{userId}` | `CanViewPermissionsForUserAsync` |
| `BaseGroupListController` | OData `odata/GroupList` (subclass named `GroupListController`) | `Get()` returning `IQueryable<GroupListItem>`, plus every OData concern |
| `BaseUserListController<TUserListItem>` | OData `odata/UserList` (subclass named `UserListController`) | `Get()` returning the host's row type, **filtered to the users the caller may see**, plus every OData concern |

```csharp
using Cause.SecurityManagement.Controllers.Management;

[UserWithPermission(Permissions.CanAccessGroups)]
public class GroupManagementController(IGroupManagementApiService service, IValidator<GroupDto> validator)
    : BaseGroupManagementController(service, validator);

[UserWithPermission(Permissions.CanAccessUsers)]
public class UserEditionController(
    IUserNameAvailabilityReader userNameAvailability,
    IValidator<UserForEdition> userValidator,
    IUserVisibilityReader visibility,
    IUserEditionService service)
    : BaseUserEditionController<User, UserForEdition>(userNameAvailability, userValidator)
{
    protected override Task<bool> CanEditUserAsync(Guid userId, CancellationToken cancellationToken)
        => visibility.CanSeeUserAsync(userId, cancellationToken);

    protected override Task<bool> CanGrantGroupsAsync(IReadOnlyCollection<Guid> groupIds, CancellationToken cancellationToken)
        => visibility.CanGrantGroupsAsync(groupIds, cancellationToken);

    protected override Task<UserForEdition?> ReadForEditionAsync(Guid userId, CancellationToken cancellationToken)
        => service.ReadForEditionAsync(userId, cancellationToken);

    protected override Task<UserSaveResult> WriteForEditionAsync(UserForEdition user, CancellationToken cancellationToken)
        => service.WriteForEditionAsync(user, cancellationToken);
}
```

`IUserVisibilityReader` and `IUserEditionService` are host types; the library has neither.

- `CanEditUserAsync` also runs on creation, with `Guid.Empty`. Decide explicitly whether any caller of
  the screen may create users.
- `POST GroupManagement` is an upsert: the client generates the `Guid` of new groups and
  group-permissions and posts the whole object; permission overrides and membership are reconciled
  against the payload.

## OData feeds

The library references no OData package. The host owns `[EnableQuery]`, the EDM and `AddOData`:

```csharp
var builder = new ODataConventionModelBuilder();
builder.EnableLowerCamelCase();
builder.EntitySet<GroupListItem>("GroupList");

services.AddControllers().AddOData(options => options
    .AddRouteComponents("odata", builder.GetEdmModel())
    .Filter().OrderBy().Count().SetMaxTop(null));

public class GroupListController(MyDbContext context) : BaseGroupListController
{
    [EnableQuery]
    public override IQueryable<GroupListItem> Get() => context.GroupListView.AsNoTracking();
}
```

The Angular grid filters with `contains(searchableGroup, '<term>')` after lower-casing the term and
stripping its diacritics. Expose every `Searchable*` column **already normalised** (lower case, no
diacritics) from a database view (PostgreSQL `unaccent()`, a MariaDB collation or `CONVERT`) mapped
with `ToView(...)`. Normalising in C# breaks server-side filtering.

## Legacy controllers — do not use in new code

- `Cause.SecurityManagement.Controllers.BaseGroupManagementController` (`api/groups`) and
  `Cause.SecurityManagement.Controllers.BasePermissionManagementController` (`api/permissions`) are
  `[Obsolete]`. Same class names as the new ones: check the `using`.
- `BaseUserManagementController<TService, TUser>(TService)` (`api/users`) is the older synchronous
  user API, still used by existing screens. Its constructor takes only the service.
