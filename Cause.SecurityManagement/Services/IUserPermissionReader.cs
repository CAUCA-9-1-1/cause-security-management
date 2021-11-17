using Cause.SecurityManagement.Models.DataTransferObjects;
using System.Collections.Generic;

namespace Cause.SecurityManagement.Services
{
    public interface IUserPermissionReader
    {
        List<AuthenticationUserPermission> GetActiveUserPermissions();
    }
}