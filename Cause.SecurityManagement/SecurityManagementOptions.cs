using Cause.SecurityManagement.Services;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Cause.SecurityManagement
{
    public class SecurityManagementOptions
    {
        internal (Type serviceType, Type implementationType)? CustomAuthenticationService { get; set; } = null;
        internal (Type serviceType, Type implementationType)? CustomUserManagementService { get; set; } = null;
        internal (Type serviceType, Type implementationType)? CustomCurrentUserService { get; set; } = null;
        internal Type ValidationCodeSender { get; set; } = null;
        internal Type ValidationCodeValidator { get; set; } = null;
        internal Type EmailForUserModificationSender { get; set; } = null;
        internal static bool MultiFactorAuthenticationIsActivated { get; set; }

        public SecurityManagementOptions()
        {
            MultiFactorAuthenticationIsActivated = false;
        }

        public void SetCustomUserManagementService<TUser, TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>()
            where TService : IUserManagementService<TUser>
            where TImplementation : class, TService
        {
            CustomUserManagementService = (typeof(TService), typeof(TImplementation));
        }

        public void SetAuthenticationService<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>()
            where TService : IAuthenticationService
            where TImplementation : class, TService
        {
            CustomAuthenticationService = (typeof(TService), typeof(TImplementation));
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
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TCheckerImplementation
        >()
        {
            MultiFactorAuthenticationIsActivated = true;
            ValidationCodeSender = typeof(TSenderImplementation);
            ValidationCodeValidator = typeof(TCheckerImplementation);
        }

        public void SendEmailWhenUserAreBeingModified<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>()
            where TImplementation : IEmailForUserModificationSender
        {
            EmailForUserModificationSender = typeof(TImplementation);
        }
    }
}