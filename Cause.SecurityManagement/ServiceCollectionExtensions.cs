using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cause.SecurityManagement
{
    public static class ServiceCollectionExtensions
	{
		public static IServiceCollection InjectSecurityServices<TUserManagementService, TUser>(this IServiceCollection services) 
		    where TUserManagementService : UserManagementService<TUser>
            where TUser : User, new()
		{
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
			services.AddScoped<IAuthenticationService, AuthenticationService<TUser>>();
			services.AddScoped<TUserManagementService>();
            services.AddScoped<IGroupManagementService, BaseGroupManagementService<TUser>>();
            services.AddScoped<IPermissionManagementService, BasePermissionManagementService<TUser>>();
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