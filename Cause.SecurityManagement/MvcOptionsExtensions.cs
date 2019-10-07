using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cause.SecurityManagement
{
    public static class MvcOptionsExtensions
    {
        public static void AskForAuthorizationByDefault(MvcOptions options)
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireRole(SecurityRoles.User)
                .Build();
            options.Filters.Add(new UseDefaultAuthorizationWhenNotSpecifiedFilter(policy));
        }

        /*public static void AskForAuthorizationUsingUserAndExternalSystemPolicies(MvcOptions options)
        {
            options.Conventions.Add(new AddAuthorizeFiltersControllerConvention());
        }*/
    }
}