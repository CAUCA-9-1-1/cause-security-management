using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Repositories;
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
                .AddScopedCustomServiceOrDefault<IUserManagementService<TUser>>(managementOptions.CustomUserManagementService, () => services.AddScoped<IUserManagementService<TUser>, UserManagementService<TUser>>())
                .AddScopedCustomServiceOrDefault<IAuthenticationService>(managementOptions.CustomAuthenticationService, () => services.AddScoped<IAuthenticationService, AuthenticationService<TUser>>())
                .AddScopedCustomServiceOrDefault<ICurrentUserService>(managementOptions.CustomCurrentUserService, () => services.AddScoped<ICurrentUserService, CurrentUserService>())
                .AddScopedWhenImplementationIsKnown<IAuthenticationValidationCodeSender<TUser>>(managementOptions.ValidationCodeSender);
        }

        private static IServiceCollection AddBaseConfiguration<TUser>(this IServiceCollection services) where TUser : User, new()
        {
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IAuthenticationMultiFactorHandler<TUser>, AuthenticationMultiFactorHandler<TUser>>();
            services.AddScoped<IMobileVersionService, MobileVersionService>();            
            services.AddScoped<IAuthenticationService, AuthenticationService<TUser>>();
            services.AddScoped<ITokenGenerator, TokenGenerator>();
            services.AddScoped<ITokenReader, TokenReader>();
            services.AddScoped<IExternalSystemAuthenticationService, ExternalSystemAuthenticationService<TUser>>();
            services.AddScoped<IGroupManagementService, BaseGroupManagementService<TUser>>();
            services.AddScoped<IPermissionManagementService, BasePermissionManagementService<TUser>>();
            services.AddScoped<IAdministratorUserGenerator, AdministratorUserGenerator<TUser>>();
            services.AddScoped<IUserPermissionRepository, UserPermissionRepository<TUser>>();
            services.AddScoped<IUserRepository<TUser>, UserRepository<TUser>>();
            services.AddScoped<IExternalSystemRepository, ExternalSystemRepository<TUser>>();
            services.AddScoped<IUserValidationCodeRepository, UserValidationCodeRepository<TUser>>();
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

        internal static IServiceCollection AddScopedWhenImplementationIsKnown<T>(this IServiceCollection services, Type serviceType)
        {
            if (serviceType != null)
            {
                services.AddScoped(typeof(T), serviceType);
            }
            return services;
        }

        internal static IServiceCollection AddScopedCustomServiceOrDefault<TDefaultService>(this IServiceCollection services, (Type serviceType, Type implementationType)? customImplementation, Action defaultImplementation = null)
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