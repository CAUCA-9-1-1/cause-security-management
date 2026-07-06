using Microsoft.AspNetCore.Authorization;

namespace Cause.SecurityManagement.Core;

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

public class AuthorizeForCertificateRoleAttribute() : AuthorizeByRolesAttribute(SecurityRoles.ApiCertificate);
public class AuthorizeForAdministratorRoleAttribute() : AuthorizeByRolesAttribute(SecurityRoles.Administrator);
public class AuthorizeForUserAndAdministratorRolesAttribute() : AuthorizeByRolesAttribute(SecurityRoles.User, SecurityRoles.Administrator);
public class AuthorizeForUserAdministratorAndCertificateRolesAttribute() : AuthorizeByRolesAttribute(SecurityRoles.User, SecurityRoles.Administrator, SecurityRoles.ApiCertificate, SecurityRoles.ExternalSystem);
public class AuthorizeForUserRolesAttributes() : AuthorizeByRolesAttribute(SecurityRoles.User);
