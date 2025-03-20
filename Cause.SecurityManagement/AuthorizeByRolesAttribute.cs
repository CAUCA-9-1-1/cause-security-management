using Microsoft.AspNetCore.Authorization;

namespace Cause.SecurityManagement;

public class AuthorizeByRolesAttribute : AuthorizeAttribute
{
    public AuthorizeByRolesAttribute(params string[] roles)
    {
        Roles = string.Join(",", roles);
    }
}

public class AuthorizeByPoliciesAttribute : AuthorizeAttribute
{
    public AuthorizeByPoliciesAttribute(params string[] policies)
    {
        Policy = string.Join(",", policies);
    }
}

public class AuthorizeForAdministratorRoleAttribute() : AuthorizeByRolesAttribute(SecurityRoles.Administrator);
public class AuthorizeForUserAndAdministratorRolesAttribute() : AuthorizeByRolesAttribute(SecurityRoles.User, SecurityRoles.Administrator);
public class AuthorizeForUserRolesAttributes() : AuthorizeByRolesAttribute(SecurityRoles.User);