using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Cause.SecurityManagement;

public class SecurityManagementOptions
{
    internal (Type serviceType, Type implementationType)? CustomAuthenticationService { get; set; }
    internal (Type serviceType, Type implementationType)? CustomUserTokenRefresher { get; set; }
    internal (Type serviceType, Type implementationType)? CustomUserManagementService { get; set; }
    internal (Type serviceType, Type implementationType)? CustomCurrentUserService { get; set; }
    internal Type ValidationCodeSender { get; set; }
    internal Type ValidationCodeValidator { get; set; }
    internal Type EmailForUserModificationSender { get; set; }
    internal Type DeviceManager { get; set; }
    internal static bool MultiFactorAuthenticationIsActivated { get; set; }
    public static bool ValidateCurrentPasswordOnPasswordChange { get; set; }
    internal static string AuthenticationScheme { get; private set; } = JwtBearerDefaults.AuthenticationScheme;

    public SecurityManagementOptions()
    {
        MultiFactorAuthenticationIsActivated = false;
        ValidateCurrentPasswordOnPasswordChange = false;
    }

    public void SetCustomUserManagementService<TUser, TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>()
        where TService : IUserManagementService<TUser>
        where TImplementation : class, TService
    {
        CustomUserManagementService = (typeof(TService), typeof(TImplementation));
    }

    public void SetUserAuthenticationService<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>()
        where TService : IUserAuthenticator
        where TImplementation : class, TService
    {
        CustomAuthenticationService = (typeof(TService), typeof(TImplementation));
    }

    public void SetUserTokenRefresher<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>()
        where TService : IUserTokenRefresher
        where TImplementation : class, TService
    {
        CustomUserTokenRefresher = (typeof(TService), typeof(TImplementation));
    }

    public void SetCurrentUserService<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>()
        where TService : ICurrentUserService
        where TImplementation : class, ICurrentUserService
    {
        CustomCurrentUserService = (typeof(TService), typeof(TImplementation));
    }

    public void UseMultiFactorAuthentication<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>()
    {
        MultiFactorAuthenticationIsActivated = true;
        ValidationCodeSender = typeof(TImplementation);
        ValidationCodeValidator = null;
    }

    public void UseMultiFactorAuthentication<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSenderImplementation,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TCheckerImplementation>()
    {
        MultiFactorAuthenticationIsActivated = true;
        ValidationCodeSender = typeof(TSenderImplementation);
        ValidationCodeValidator = typeof(TCheckerImplementation);
    }

    public void UseMultiFactorAuthentication()
    {
        MultiFactorAuthenticationIsActivated = true;
    }

    public void UseCurrentPasswordValidationOnPasswordChange()
    {
        ValidateCurrentPasswordOnPasswordChange = true;
    }

    public void UseCustomAuthenticationScheme(string scheme)
    {
        AuthenticationScheme = scheme;
    }

    public void SendEmailWhenUserAreBeingModified<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>()
        where TImplementation : IEmailForUserModificationSender
    {
        EmailForUserModificationSender = typeof(TImplementation);
    }

    public void ManageDeviceWhenCreatingRegularUser<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>()
        where TImplementation : IDeviceManager
    {
        DeviceManager = typeof(TImplementation);
    }
}