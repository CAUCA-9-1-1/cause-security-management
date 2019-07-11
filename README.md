# Cause.SecurityManagement
[![NuGet version](https://badge.fury.io/nu/Cause.SecurityManagement.svg)](https://badge.fury.io/nu/Cause.SecurityManagement)

Add basic JWT authentification to an Asp.Net core project.

# Usage
### appsettings.json
Some configuration needs to be added:
```json
 "APIConfig": {
      "Issuer": "http://www.somewebsite.com/",
      "PackageName": "My-Application-Name",
      "SecretKey": "MySuperSecretEncodingKey"
  },
```
You can change any of of these values to anything you want.

### Models
You need to have a `User` model that inherits from `SecurityManagement.Models.User`.  You can add whatever you need to your model.

Your 'User' mapping needs to inherit from `UserMapping<T>`.  You can override `Map(EntityTypeBuilder<T> model)` to do your mapping. 

### DbContext
Your DbContext needs to inherits from `BaseSecurityContext<T>`

In OnModelCreating, you need to add the Security Management's mapping like this: 
```
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
		public UserManagementController(UserManagementService managementService, IConfiguration configuration)
			: base(managementService, configuration)
		{			
		}
	}
```

You can add whatever features you need to this file to the basic one that comes with it.

##### Default function:
 - HttpGet /api/users/ : get all user entities.
 - HttpGet /api/users/{id} : get user entity.
 - HttpPost /api/users : Add or update user.
 - HttpDelete /api/users/{id} : delete user.
 - HttpGet /api/users/{id}/groups : get user's groups.
 - HttpPost /api/users/group : add group to user.
 - HttpDelete /api/users/groups/{id} : remove group from user.
 - HttpPost /api/users/permission : add permission to user.
 - HttpDelete /permissions/{id} : remove permission from user.
 - HttpPost: /password : change user password.

### Startup.cs
You first need to inject security services  in `ConfigureServices`.  In this example, the authentification will be done using the same database as the main context:

```cs
services.InjectSecurityServices<UserManagementService<User>, User>;
```

Still in `ConfigureServices`, you need to configure your API to use Token Authentication:
```cs
services.AddTokenAuthentification(Configuration);
```

Then, you need to inject the security controller to you services in `ConfigureServices`:
```cs
services.AddMvc(...)
    .InjectSecurityControllers();
```
> This will inject the AuthenficationController 

Last, you need set the app to use the authentication from the `Configure` function:
```cs
app.UseAuthentication();
```
##### Admin user
You can also set Startup.cs to make it create the admin user when it does not exist.  This is a way to do it from the `ConfigureServices` function :
```cs
var service = services.BuildServiceProvider()
	.GetService<AuthentificationService>();
var applicationName = Configuration.GetSection("APIConfig:PackageName").Value;
service.EnsureAdminIsCreated(applicationName);
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

# Limitation
### UserGroups and permissions
This is has not been tested, so use at your own risk.
