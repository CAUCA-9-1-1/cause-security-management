﻿using System;
using Cause.SecurityManagement.Models.DataTransferObjects;
using System.Collections.Generic;
using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Repositories
{
    public interface IUserPermissionRepository
    {
        List<AuthenticationUserPermission> GetActiveUserPermissions();
        List<UserPermission> GetForUser(Guid userId);
        UserPermission Get(Guid userPermissionId);
        bool Any(Guid userPermissionId);
        void Add(UserPermission userPermission);
        void Remove(UserPermission userPermission);
        void Update(UserPermission userPermission);
        void SaveChanges();

    }
}