using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Cause.SecurityManagement
{
    public static class ServiceCollectionExtensions
	{
		public static IServiceCollection InjectSecurityServices<TUser>(this IServiceCollection services, Action<SecurityManagementOptions> options = null) 
            where TUser : User, new()
        {
            var managementOptions = new SecurityManagementOptions();
            options?.Invoke(managementOptions);

            return services
                .AddBaseConfiguration<TUser>()
                .AddCustomServiceOrDefault<IUserManagementService<TUser>>(managementOptions.CustomUserManagementService, () => services.AddScoped<IUserManagementService<TUser>, UserManagementService<TUser>>())
                .AddCustomServiceOrDefault<IAuthenticationService>(managementOptions.CustomAuthenticationService, () => services.AddScoped<IAuthenticationService, AuthenticationService<TUser>>())
                .AddCustomServiceOrDefault<ICurrentUserService>(managementOptions.CustomCurrentUserService, () => services.AddScoped<ICurrentUserService, CurrentUserService>());
        }

        private static IServiceCollection AddBaseConfiguration<TUser>(this IServiceCollection services) where TUser : User, new()
        {
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IAuthenticationService, AuthenticationService<TUser>>();
            services.AddScoped<IExternalSystemAuthenticationService, ExternalSystemAuthenticationService<TUser>>();
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

        internal static IServiceCollection AddCustomServiceOrDefault<TDefaultService>(this IServiceCollection services, (Type serviceType, Type implementationType)? customImplementation, Action defaultImplementation)
        {
            if (customImplementation.HasValue)
            {
                var (serviceType, implementationType) = customImplementation.Value;
                services.AddScoped(serviceType, implementationType);
                if (serviceType != typeof(TDefaultService))
                    services.AddScoped(typeof(TDefaultService), implementationType);
            }
            else
            {
                defaultImplementation?.Invoke();
            }
            return services;
        }
    }
}