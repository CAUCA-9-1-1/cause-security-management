using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cause.SecurityManagement
{
    public static class ServiceCollectionExtensions
	{
		public static IServiceCollection InjectSecurityServices<TUserManagementService, TUser>(this IServiceCollection services) 
		    where TUserManagementService : UserManagementService<TUser>
            where TUser : User, new()
		{
			services.AddScoped<CurrentUser>();
			services.AddTransient<IAuthenticationService, AuthenticationService<TUser>>();
			services.AddTransient<TUserManagementService>();
            services.AddTransient<IGroupManagementService, BaseGroupManagementService<TUser>>();
            services.AddTransient<IPermissionManagementService, BasePermissionManagementService<TUser>>();
            return services;
		}

        public static IServiceCollection AddExternalSystemAndUserPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(c =>
            {
                c.AddPolicy("defaultpolicy", b =>
                {
                    b.RequireAuthenticatedUser();
                    b.RequireRole(SecurityRoles.User);
                });
                c.AddPolicy("apipolicy", b =>
                {
                    b.RequireAuthenticatedUser();
                    b.RequireRole(SecurityRoles.ExternalSystem);
                });
            });
            return services;
        }
    }
}