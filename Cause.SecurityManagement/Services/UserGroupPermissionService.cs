using System;
using System.Collections.Generic;
using System.Linq;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Repositories;
using Microsoft.Extensions.Options;

namespace Cause.SecurityManagement.Services
{
    public class UserGroupPermissionService : IUserGroupPermissionService
    {
        private readonly ICurrentUserService currentUserService;
        private readonly IGroupRepository groupRepository;
        private readonly IUserPermissionService userPermissionService;
        private readonly SecurityConfiguration configuration;

        public UserGroupPermissionService(
            ICurrentUserService currentUserService,
            IGroupRepository groupRepository,
            IUserPermissionService userPermissionService,
            IOptions<SecurityConfiguration> configuration)
        {
            this.currentUserService = currentUserService;
            this.groupRepository = groupRepository;
            this.userPermissionService = userPermissionService;
            this.configuration = configuration.Value;
        }

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
