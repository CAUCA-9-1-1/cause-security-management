using System;

namespace Cause.SecurityManagement.Services
{
    public interface ICurrentUserService
    {
        Guid GetUserId();
        string GetUserIpAddress();
    }
}
