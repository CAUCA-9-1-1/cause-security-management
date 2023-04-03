using Cause.SecurityManagement.Models.DataTransferObjects;
using System;
using System.Collections.Generic;

namespace Cause.SecurityManagement.Services
{
    public interface ICurrentUserService
    {
        Guid GetUserId();
        string GetUserIpAddress();
        List<AuthenticationUserPermission> GetPermissions();
    }
}
