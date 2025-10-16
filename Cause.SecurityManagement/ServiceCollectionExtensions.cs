using Cause.SecurityManagement.Authentication.Certificate;
using Cause.SecurityManagement.Authentication.MultiFactor;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Repositories;
using Cause.SecurityManagement.Services;
using Cause.SecurityManagement.VersionManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Cause.SecurityManagement;

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
            .AddScopedCustomServiceOrDefault<IUserAuthenticator>(managementOptions.CustomAuthenticationService, () => services.AddScoped<IUserAuthenticator, UserAuthenticator<TUser>>())
            .AddScopedCustomServiceOrDefault<IUserTokenRefresher>(managementOptions.CustomUserTokenRefresher, () => services.AddScoped<IUserTokenRefresher, UserTokenRefresher<TUser>>())
            .AddScopedCustomServiceOrDefault<ICurrentUserService>(managementOptions.CustomCurrentUserService, () => services.AddScoped<ICurrentUserService, CurrentUserService>())
            .AddScopedWhenImplementationIsKnown<IAuthenticationValidationCodeSender<TUser>>(managementOptions.ValidationCodeSender)
            .AddScopedWhenImplementationIsKnown<IAuthenticationValidationCodeValidator<TUser>>(managementOptions.ValidationCodeValidator)
            .AddScopedWhenImplementationIsKnown<IEmailForUserModificationSender>(managementOptions.EmailForUserModificationSender)
            .AddScopedWhenImplementationIsKnown<IDeviceManager>(managementOptions.DeviceManager);
    }

    private static IServiceCollection AddBaseConfiguration<TUser>(this IServiceCollection services) where TUser : User, new()
    {
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<IAuthenticationMultiFactorHandler<TUser>, AuthenticationMultiFactorHandler<TUser>>();
        services.AddScoped<IMobileVersionValidator, MobileVersionValidator>();
        services.AddScoped<ITokenGenerator, TokenGenerator>();
        services.AddScoped<ITokenReader, TokenReader>();
        services.AddScoped<IExternalSystemAuthenticationService, ExternalSystemAuthenticationService>();
        services.AddScoped<IGroupManagementService, BaseGroupManagementService>();
        services.AddScoped<IPermissionManagementService, BasePermissionManagementService<TUser>>();
        services.AddScoped<IAdministratorUserGenerator, AdministratorUserGenerator<TUser>>();
        services.AddScoped<IUserPermissionRepository, UserPermissionRepository<TUser>>();
        services.AddScoped<IUserRepository<TUser>>(provider => new UserRepository<TUser>(provider.GetRequiredService<IScopedDbContextProvider<TUser>>()));
        services.AddScoped<IGroupRepository, GroupRepository<TUser>>();
        services.AddScoped<IUserGroupRepository, UserGroupRepository<TUser>>();
        services.AddScoped<IGroupPermissionRepository, GroupPermissionRepository<TUser>>();
        services.AddScoped<IUserGroupPermissionService, UserGroupPermissionService>();
        services.AddScoped<IUserPermissionService, UserPermissionService>();
        services.AddScoped<IExternalSystemRepository, ExternalSystemRepository<TUser>>();
        services.AddScoped<IUserValidationCodeRepository, UserValidationCodeRepository<TUser>>();
        services.AddScoped<IScopedDbContextProvider<TUser>, ScopedDbContextProvider<TUser>>();
        services.AddScoped<ICertificateValidator, CertificateValidator>();
        services.AddScoped<IUserTokenGenerator, UserTokenGenerator<TUser>>();
        return services;
    }

    public static IServiceCollection AddBasicPoliciesForCertificateLogon(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("defaultpolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(SecurityRoles.User);
            });
            options.AddPolicy("apipolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(SecurityRoles.ExternalSystem);
            });
            options.AddPolicy("apicertificatepolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(SecurityRoles.ExternalSystem);
                policy.AddAuthenticationSchemes(CertificateAuthenticationOptions.Name, SecurityManagementOptions.AuthenticationScheme);
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