using Cause.SecurityManagement.Models.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Services;

public interface ICurrentUserService
{
    Guid GetUserId();
    Guid? GetExternalSystemId();
    string GetUserIpAddress();
    Task<List<AuthenticationUserPermission>> GetPermissionsAsync();
    Guid? GetUserDeviceId();
    string GetAuthentifiedUserIdentifier();
    string GetRole();
}