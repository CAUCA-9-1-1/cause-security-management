using System;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Repositories;
using Microsoft.Extensions.Options;

namespace Cause.SecurityManagement.Services
{
    public class UserGroupPermissionService<TUser> : IUserGroupPermissionService
    {
        private readonly ICurrentUserService currentUserService;
        private readonly IUserManagementService<TUser> userManagementService;
        private readonly IGroupRepository groupRepository;
        private readonly SecurityConfiguration configuration;

        public UserGroupPermissionService(
            ICurrentUserService currentUserService,
            IUserManagementService<TUser> userManagementService,
            IGroupRepository groupRepository,
            IOptions<SecurityConfiguration> configuration)
        {
            this.currentUserService = currentUserService;
            this.userManagementService = userManagementService;
            this.groupRepository = groupRepository;
            this.configuration = configuration.Value;
        }

        public bool CurrentUserHasRequiredPermissionForAllGroupsAccess()
        {
            return string.IsNullOrWhiteSpace(configuration.RequiredPermissionForAllGroupsAccess)
                   || userManagementService.HasPermission(currentUserService.GetUserId(), configuration.RequiredPermissionForAllGroupsAccess);
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
