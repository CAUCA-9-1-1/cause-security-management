using Microsoft.AspNetCore.Authorization;

namespace Cause.SecurityManagement;

public class AuthorizeByRolesAttribute : AuthorizeAttribute
{
    public AuthorizeByRolesAttribute(params string[] roles)
    {
        Roles = string.Join(",", roles);
    }
}