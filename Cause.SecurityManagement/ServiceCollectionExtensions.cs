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
			services.AddTransient<IAuthentificationService, AuthentificationService<TUser>>();
			services.AddTransient<TUserManagementService>();
            services.AddTransient<IGroupManagementService, BaseGroupManagementService<TUser>>();
            services.AddTransient<IPermissionManagementService, BasePermissionManagementService<TUser>>();
            return services;
		}        
    }
}