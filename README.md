# Cause.SecurityManagement

Add basic JWT authentication to an Asp.Net core project.

| Package | NuGet |
|---------|-------|
| `Cause.SecurityManagement.Core` | [![NuGet version](https://badge.fury.io/nu/Cause.SecurityManagement.Core.svg)](https://badge.fury.io/nu/Cause.SecurityManagement.Core) |
| `Cause.SecurityManagement` | [![NuGet version](https://badge.fury.io/nu/Cause.SecurityManagement.svg)](https://badge.fury.io/nu/Cause.SecurityManagement) |
| `Cause.SecurityManagement.Wolverine` | [![NuGet version](https://badge.fury.io/nu/Cause.SecurityManagement.Wolverine.svg)](https://badge.fury.io/nu/Cause.SecurityManagement.Wolverine) |

# Usage
### appsettings.json
Some configuration needs to be added:
```json
 "APIConfig": {
      "Issuer": "http://www.somewebsite.com/",
      "PackageName": "My-Application-Name",
      "SecretKey": "MySuperSecretEncodingKey",
      "CertificateIssuers": [],
      "MinimalVersion": "1.0.0",
  },
  "KeycloakConfig": {
      "Url": "https://keycloak.example.com",
      "Realm": "my-realm",
      "ClientId": "my-client",
      "ClientSecret": "my-secret"
  }
```
You can change any of of these values to anything you want.  

Three more settings can also be added in the APIConfig section when needed and have these default values: 
- "AccessTokenLifeTimeInMinutes": 540
- "RefreshTokenLifeTimeInMinutes": 60
- "RefreshTokenCanExpire": true
- "AllowRefreshWithExpiredToken": true

You can also specify a permission to allow a user to login:
- "RequiredPermissionForLogin": "permissionName".


### Models
You need to have a `User` model that inherits from `SecurityManagement.Models.User`.  You can add whatever you need to your model.

Your 'User' mapping needs to inherit from `UserMapping<T>`.  You can override `Map(EntityTypeBuilder<T> model)` to do your mapping. 

### DbContext
Your DbContext needs to inherits from `BaseSecurityContext<T>`

In OnModelCreating, you need to add the Security Management's mapping like this: 
```cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    AddSecurityManagementMappings(modelBuilder);
    (...)
}
```

### Data Service
Your UserService should inherit from `UserManagementService<T>`

### Users management controller
You may want to add users management feature to you application, so you will need to create a controller that inherits from the BaseUserManagementController.  This is one example :
```cs
	[Route("api/[Controller]"), ApiController]
	public class UserManagementController : BaseUserManagementController<UserManagementService<User>, User>
	{
		public UserManagementController(UserManagementService managementService, IOptions<SecurityConfiguration> configuration)
			: base(managementService, configuration)
		{			
		}
	}
```

You can add whatever features you need to this file to the basic one that comes with it.

### Startup.cs
You first need to inject security services  in `ConfigureServices`.  In this example, the authentication will be done using the same database as the main context:

```cs
services.Configure<SecurityConfiguration>(Configuration.GetSection("APIConfig"));
services.InjectSecurityServices<UserManagementService<User>, User>();
```

Still in `ConfigureServices`, you need to configure your API to use Token Authentication:
```cs
var securityConfiguration = Configuration.GetSection(nameof(SecurityConfiguration)).Get<SecurityConfiguration>();
services.AddTokenAuthentication(securityConfiguration);
```

Then, you need to inject the security controller to you services in `ConfigureServices`:
```cs
services.AddMvc(...).InjectSecurityControllers();
```
> This will inject the AuthenticationController 

Last, you need set the app to use the authentication from the `Configure` function:
```cs
app.UseAuthentication();
```
##### Admin user
You can also set Startup.cs to make it create the admin user when it does not exist.  This is a way to do it from the `ConfigureServices` function :
```cs
var service = services.BuildServiceProvider()
	.GetService<AuthenticationService>();
service.EnsureAdminIsCreated();
```

##### Make all controllers ask for authorization by default
It is a good idea to make all controllers ask for authentication by default.  You can use this function to do it :
```cs
private static void AskForAuthorizationByDefault(MvcOptions options)
{
	var policy = new AuthorizationPolicyBuilder()
		.RequireAuthenticatedUser()
		.Build();
	options.Filters.Add(new AuthorizeFilter(policy));
}
```

You can then use this function as an option when calling `AddMvc` from `ConfigureServices`.
```cs
services.AddMvc(options => AskForAuthorizationByDefault(options));
```
> Note: you can use the `AllowAnonymous` attribute on any controller's function if you want to allow anonymous usage.

# Service Collection Extensions

The library provides several extension methods to simplify the configuration of security services, authentication, and authorization.

## Core Services

### `InjectSecurityServices<TUser>`
Injects the core security services required by the library, including user management, authentication services, repositories, and validators. 

```csharp
services.InjectSecurityServices<User>(options => {
    options.UseMultiFactorAuthentication();
    options.SetValidationCodeSender<MySmsSender>();
});
```

You can customize specific services via the `SecurityManagementOptions` delegate:
*   `UseMultiFactorAuthentication()`: Activates MFA.
*   `SetValidationCodeSender<T>()`: Sets the service to send validation codes.
*   `SetValidationCodeValidator<T>()`: Sets the service to validate codes.
*   `SetEmailForUserModificationSender<T>()`: Sets the service to send emails when users are modified.
*   `SetCustomUserManagementService<T>()`: Overrides the default user management service.
*   `SetCustomAuthenticationService<T>()`: Overrides the default user authentication service.

## Authentication

### `AddTokenAuthentication`
Adds dual token authentication support: Keycloak and internal user. It configures JWT bearer authentication for both schemes.

### `AddTokenAuthenticationWithCertificates`
Adds triple token authentication support: Keycloak, internal user, and console certificates. It configures JWT bearer authentication and allows specifying a custom authentication handler for certificates.

### `AddSimpleTokenAuthentication`
Adds simple token authentication using JWT bearer based on the provided security configuration.

### `AddExternalCertificateAuthentication`
Adds certificate authentication for external systems using the default handler.

## Authorization

### `AddAuthorizationForRegularUser`
Adds authorization policies specifically for regular users, including policies for user recovery, creation, password setup, and metrics.

### `AddAuthorizationForRegularUserAndExternalSystem`
Adds authorization policies for regular users and external systems. Includes policies for user recovery, creation, password setup, and metrics.

### `AddAuthorizationForKeycloakAndRegularUserSchemes`
Adds authorization policies for Keycloak and regular users, including a policy for metrics. It does not include user management policies.

### `AddAuthorizationForRegularUserKeycloakAndApiCertificate`
Adds authorization policies for regular users, Keycloak users, and API certificates. It includes default policies for user recovery, user creation, password setup, and metrics.

# Custom Authentication Controller

You can implement authentication for a custom source (e.g., external system, different user table) by inheriting from `BaseAuthenticationController`.

1.  **Create a Controller**: Create a new controller that inherits from `BaseAuthenticationController`.
2.  **Implement Authenticator**: Implement `IEntityAuthenticator` (or `IUserAuthenticator`) to handle your specific login logic.
3.  **Implement Token Refresher**: Implement `IEntityTokenRefresher` to handle token refreshing logic.
4.  **Inject Services**: Inject your custom implementations into the controller's constructor.

```csharp
[Route("api/[Controller]")]
[ApiController]
public class MyCustomAuthController : BaseAuthenticationController
{
    public MyCustomAuthController(
        IMyCustomAuthenticator authenticator, 
        IMyCustomTokenRefresher tokenRefresher,
        ILogger<AuthenticationController> logger)
        : base(authenticator, tokenRefresher, logger)
    {
    }
}
```

---

# Group & Permission Management API

The library provides the backend consumed by the `@cauca-911/cauca-management` Angular library
(the modernized group & permission management UI). It ships **abstract, fully asynchronous**
controllers that the host application subclasses to activate, plus the data contract and services.
Every action takes the request's `CancellationToken` and threads it all the way down to the EF Core
queries, so a cancelled HTTP request never runs (or keeps running) its database work.

The services are registered automatically by `InjectSecurityServices<TUser>()`:
`IGroupManagementApiService`, `IUserSearchService`, `IPermissionCatalogService` and the
`IValidator<GroupDto>`.

## REST controllers

Each controller has a single responsibility, so there is one abstract base per concern. Subclass
them — the routes are inherited from the base, so an empty subclass is enough to expose them at the
routes the Angular library expects:

```csharp
using Cause.SecurityManagement.Controllers.Management;

public class GroupManagementController(
    IGroupManagementApiService service,
    IValidator<GroupDto> validator)
    : BaseGroupManagementController(service, validator);

public class UserSearchController(IUserSearchService service)
    : BaseUserSearchController(service);

public class PermissionManagementController(IPermissionCatalogService service)
    : BasePermissionManagementController(service);
```

They are activated like the other library controllers, via `InjectSecurityControllers()`, and pick
up the project's default authorization (an authenticated user is required — no `[AllowAnonymous]`).

| Method | Route | Description |
|--------|-------|-------------|
| `DELETE` | `GroupManagement/{groupId}` | Delete a group and its dependents (`204` / `404`) |
| `POST`   | `GroupManagement` | **Upsert** a group (client generates the ids), its permission overrides and its membership (`200` / `400`) |
| `GET`    | `GroupManagement/{groupId}` | Full group payload (`200` / `404`) |
| `POST`   | `UserSearch` | Server-side paged search over active users (group member selection) |
| `GET`    | `PermissionManagement` | The assignable module-permission catalog |

> `POST GroupManagement` is an upsert: the client generates the `Guid` for new groups and new
> group-permissions, then posts the whole object. The group is inserted when its id is unknown and
> updated otherwise; permission overrides and membership are reconciled against the payload.

## Groups OData feed (owned by the consumer)

The groups data grid is powered by an OData v4 feed at `GET odata/GroupList`. To keep the library
database-agnostic and free of any OData dependency, it provides only the contract shape
`Cause.SecurityManagement.Models.GroupListItem` (`Id`, `Name`, `AssignableByAllUsers`,
`SearchableGroup`, `SearchableUsers`) and the **abstract** `BaseGroupListController`:

```csharp
public abstract class BaseGroupListController : ControllerBase
{
    public abstract IQueryable<GroupListItem> Get();
}
```

The **consuming application owns every OData concern** — `[EnableQuery]`, the EDM model and the
`AddOData` routing. Subclass `BaseGroupListController`, name the subclass `GroupListController` so it
matches the `GroupList` entity set, and implement `Get()`.

The Angular grid filters with
`contains(searchableGroup, '<term>') or contains(searchableUsers, '<term>')`, and the client
lower-cases **and** strips diacritics from the term before sending it. OData `contains` is an exact
substring match, so your feed must expose `SearchableGroup` / `SearchableUsers` **already normalized**
(lower-cased and diacritic-free). Because diacritic stripping is not portable across database engines,
produce these columns in a database view (e.g. PostgreSQL `unaccent()`, MariaDB collation/`CONVERT`)
and map `GroupListItem` to it with `ToView(...)`.

```csharp
// EDM
private static IEdmModel GetEdmModel()
{
    var builder = new ODataConventionModelBuilder();
    builder.EnableLowerCamelCase();
    builder.EntitySet<GroupListItem>("GroupList");
    // … your own entity sets …
    return builder.GetEdmModel();
}

// Registration
services.AddControllers()
    .AddOData(options => options
        .AddRouteComponents("odata", GetEdmModel())
        .Filter().OrderBy().Count().SetMaxTop(null));

// Controller — subclass BaseGroupListController; your normalized view/projection backs the queryable
public class GroupListController(MyDbContext context) : BaseGroupListController
{
    [EnableQuery]
    public override IQueryable<GroupListItem> Get() => context.GroupListView.AsNoTracking();
}
```

> The legacy `BaseGroupManagementController` (`api/groups`) and `BasePermissionManagementController`
> (`api/permissions`) are marked `[Obsolete]` and point at the replacements above.

---

# Cause.SecurityManagement.Wolverine

A separate NuGet package (`Cause.SecurityManagement.Wolverine`) that provides Wolverine HTTP endpoints and sagas as an alternative to the MVC-based `Cause.SecurityManagement` package. Use this when your API is built on [Wolverine](https://wolverine.netlify.app/) instead of (or alongside) traditional ASP.NET Core controllers.

## Installation

```
dotnet add package Cause.SecurityManagement.Wolverine
```

This package depends on `Cause.SecurityManagement.Core` and `WolverineFx.Http`. You do **not** need `Cause.SecurityManagement` (the HTTP/MVC package).

## Setup

### 1. Register core security services

Same as the Http package — call `InjectSecurityServices` before Wolverine:

```csharp
builder.Services.Configure<SecurityConfiguration>(builder.Configuration.GetSection("APIConfig"));
builder.Services.InjectSecurityServices<UserAuthenticator<User>, User>(options =>
{
    options.UseMultiFactorAuthentication();
    options.SetValidationCodeSender<MySmsSender>();
});
```

### 2. Add Wolverine with security handlers

Call `AddSecurityManagementHandlers()` inside your `UseWolverine` configuration. This registers all endpoint classes and sagas from the package via Wolverine's assembly scanning:

```csharp
builder.Host.UseWolverine(opts =>
{
    opts.AddSecurityManagementHandlers();
});
```

### 3. Map Wolverine HTTP endpoints

In your `WebApplication` pipeline, map the Wolverine HTTP endpoints as you would normally:

```csharp
var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapWolverineEndpoints();

app.Run();
```

### Minimal example `Program.cs`

```csharp
using Cause.SecurityManagement.Core;
using Cause.SecurityManagement.Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SecurityConfiguration>(builder.Configuration.GetSection("APIConfig"));
builder.Services.InjectSecurityServices<UserAuthenticator<User>, User>();

// Add JWT authentication
var secConfig = builder.Configuration.GetSection("APIConfig").Get<SecurityConfiguration>();
builder.Services.AddTokenAuthentication(secConfig);
builder.Services.AddAuthorizationForRegularUser();

builder.Host.UseWolverine(opts =>
{
    opts.AddSecurityManagementHandlers();
    // your other Wolverine configuration …
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapWolverineEndpoints();
app.Run();
```

## Provided HTTP endpoints

All routes mirror the ones provided by `Cause.SecurityManagement` so existing clients are fully compatible.

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/api/Authentication/Logon` | Anonymous | Login with body or base64 `auth` header |
| `POST` | `/api/Authentication/Refresh` | Anonymous | Refresh user access token |
| `GET`  | `/api/Authentication/validationCode` | `UserLoginWithMultiFactor` | Send new MFA code |
| `POST` | `/api/Authentication/ValidationCode` | `UserLoginWithMultiFactor` | Verify MFA code |
| `POST` | `/api/Authentication/State` | — | Check if refresh token is still valid |
| `POST` | `/api/Authentication/RecoverAccount` | Anonymous | Initiate account recovery |
| `POST` | `/api/Authentication/RecoverAccountValidation` | Anonymous | Validate recovery code |
| `POST` | `/api/Authentication/PasswordSetup` | `RegularUser / UserPasswordSetup / UserRecovery` | Set password |
| `GET`  | `/api/Authentication/Permissions` | `RegularUser / Administrator` | Get current user permissions |
| `GET`  | `/api/Authentication/VersionValidator/{v}/Latest` | Anonymous | Check if version is latest |
| `GET`  | `/api/Authentication/VersionValidator/{v}` | Anonymous | Check if version is valid |
| `POST` | `/api/ExternalSystemAuthentication/Logon` | Anonymous | External system login |
| `POST` | `/api/Authentication/LogonForExternalSystem` | Anonymous | Legacy alias |
| `POST` | `/api/ExternalSystemAuthentication/Refresh` | Anonymous | External system token refresh |
| `POST` | `/api/Authentication/RefreshForExternalSystem` | Anonymous | Legacy alias |
| `GET`  | `/api/KeycloakConfiguration` | Anonymous | Returns Keycloak config for web clients |
| `GET`  | `/api/Me` | `RegularUser / Administrator / Console` | Returns current user claims |
| `DELETE` | `/GroupManagement/{groupId}` | Authenticated | Delete a group and its dependents |
| `POST` | `/GroupManagement` | Authenticated | Upsert a group, its permissions and membership |
| `GET`  | `/GroupManagement/{groupId}` | Authenticated | Full group payload |
| `POST` | `/UserSearch` | Authenticated | Paged active-user search (group member selection) |
| `GET`  | `/PermissionManagement` | Authenticated | Assignable module-permission catalog |

The group & permission management endpoints mirror the routes of the MVC package (see
[Group & Permission Management API](#group--permission-management-api)) and reuse the same
asynchronous, cancellation-aware services. The groups OData feed remains the consumer's
responsibility in both hosting models.

## Sagas

The package includes two Wolverine sagas for multi-step authentication flows. These are useful when you want to drive the login or recovery flow via the message bus rather than directly from HTTP endpoints (e.g. for background processing, retries, or custom orchestration).

### `UserLoginSaga`

Manages the multi-factor authentication login flow. Correlates on the authenticated user's `Guid` ID.

```
StartUserLogin(UserId)
    → saga created, MFA code already sent by LoginAsync
ResendLoginCode(UserId, CommunicationType)
    → new code sent
VerifyLoginCode(UserId, Code)
    → returns LoginResult, saga completed
```

**Usage** — publish from your own code after a login that returns `MustVerifyCode = true`:

```csharp
// After calling LoginAsync and seeing MustVerifyCode is true:
await bus.PublishAsync(new StartUserLogin(loginResult.IdUser));

// When user submits the code:
var fullResult = await bus.InvokeAsync<LoginResult>(new VerifyLoginCode(userId, submittedCode));
```

### `AccountRecoverySaga`

Manages the account recovery flow. Correlates on the trimmed username / email string.

```
StartAccountRecovery(UsernameOrEmail)
    → saga created, recovery code sent via email
ValidateAccountRecovery(UsernameOrEmail, ValidationCode)
    → returns LoginResult (MustChangePassword = true), saga completed
```

**Usage**:

```csharp
await bus.PublishAsync(new StartAccountRecovery(email));

// When user submits the recovery code:
var recoveryToken = await bus.InvokeAsync<LoginResult>(new ValidateAccountRecovery(email, code));
// recoveryToken.MustChangePassword == true — direct user to password setup
```

## Architecture

The Wolverine package uses a **vertical slices** layout. Each feature is fully self-contained in a single file under `Features/`:

```
Features/
  Authentication/
    Logon.cs
    Refresh.cs
    SendValidationCode.cs
    VerifyValidationCode.cs
    GetAuthenticationState.cs
    RecoverAccount.cs
    ValidateRecoverAccount.cs
    SetPassword.cs
    GetPermissions.cs
    MobileVersionEndpoints.cs
  ExternalSystem/
    Logon.cs
    Refresh.cs
  Keycloak/
    GetConfiguration.cs
  Me/
    GetCurrentUser.cs
  Sagas/
    Login/
      LoginMessages.cs       ← StartUserLogin, ResendLoginCode, VerifyLoginCode
      UserLoginSaga.cs
    Recovery/
      RecoveryMessages.cs    ← StartAccountRecovery, ValidateAccountRecovery
      AccountRecoverySaga.cs
```

