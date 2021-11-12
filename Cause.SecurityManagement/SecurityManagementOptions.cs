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
    }
}