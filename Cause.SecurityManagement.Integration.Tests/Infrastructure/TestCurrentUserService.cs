using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Core;

using Cause.SecurityManagement.Core.Services;

using System;

using System.Collections.Generic;

using System.Threading.Tasks;

namespace Cause.SecurityManagement.Integration.Tests.Infrastructure;

/// <summary>
/// A test double for ICurrentUserService that can be pre-configured with a specific user ID,
/// avoiding any dependency on a real HTTP context.
/// </summary>
public class TestCurrentUserService : ICurrentUserService
{
    public Guid UserId { get; set; } = Guid.Empty;
    public string Role { get; set; } = SecurityRoles.User;

    public Guid GetUserId() => UserId;
    public Guid? GetExternalSystemId() => null;
    public string GetUserIpAddress() => "127.0.0.1";
    public Guid? GetUserDeviceId() => null;
    public string GetAuthentifiedUserIdentifier() => UserId.ToString();
    public string GetRole() => Role;

    public Task<List<AuthenticationUserPermission>> GetPermissionsAsync()
        => Task.FromResult(new List<AuthenticationUserPermission>());
}
