using Cause.SecurityManagement.Models.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Interfaces.Services
{
    public interface ICurrentUserService
    {
        Guid GetUserId();
        string GetUserIpAddress();
        Task<List<AuthenticationUserPermission>> GetPermissionsAsync();
    }
}
