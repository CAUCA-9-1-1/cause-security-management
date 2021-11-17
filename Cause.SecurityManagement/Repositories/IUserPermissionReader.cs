using Cause.SecurityManagement.Models.DataTransferObjects;
using System.Collections.Generic;

namespace Cause.SecurityManagement.Repositories
{
    public interface IUserPermissionRepository
    {
        List<AuthenticationUserPermission> GetActiveUserPermissions();
    }
}