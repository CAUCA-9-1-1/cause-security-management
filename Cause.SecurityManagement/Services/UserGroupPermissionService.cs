using System;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Repositories;
using Microsoft.Extensions.Options;

namespace Cause.SecurityManagement.Services
{
    public class UserGroupPermissionService(
        ICurrentUserService currentUserService,
        IGroupRepository groupRepository,
        IUserPermissionService userPermissionService,
        IOptions<SecurityConfiguration> configuration)
        : IUserGroupPermissionService
    {
        private readonly SecurityConfiguration configuration = configuration.Value;

        public bool CurrentUserHasRequiredPermissionForAllGroupsAccess()
        {
            return string.IsNullOrWhiteSpace(configuration.RequiredPermissionForAllGroupsAccess)
                   || userPermissionService.HasPermission(currentUserService.GetUserId(), configuration.RequiredPermissionForAllGroupsAccess);
        }

        public bool CurrentUserHasRequiredPermissionForGroupsAccess(Guid groupId)
        {
            return CurrentUserHasRequiredPermissionForGroupsAccess(groupRepository.Get(groupId));
        }

        public bool CurrentUserHasRequiredPermissionForGroupsAccess(Group group)
        {
            return CurrentUserHasRequiredPermissionForAllGroupsAccess() || group.AssignableByAllUsers;
        }
    }
}
