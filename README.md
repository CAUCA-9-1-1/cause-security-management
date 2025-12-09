# Cause.SecurityManagement
[![NuGet version](https://badge.fury.io/nu/Cause.SecurityManagement.svg)](https://badge.fury.io/nu/Cause.SecurityManagement)

Add basic JWT authentication to an Asp.Net core project.

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

