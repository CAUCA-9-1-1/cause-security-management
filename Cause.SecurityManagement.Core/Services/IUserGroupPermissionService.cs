using System;
using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Core.Services
{
    public interface IUserGroupPermissionService
    {
        bool CurrentUserHasRequiredPermissionForAllGroupsAccess();
        public bool CurrentUserHasRequiredPermissionForGroupsAccess(Guid groupId);
        bool CurrentUserHasRequiredPermissionForGroupsAccess(Group group);
    }
}
