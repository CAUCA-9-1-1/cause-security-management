﻿using Cause.SecurityManagement.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Cause.SecurityManagement
{
    public static class ModelBuilderExtensions
    {
        public static void AddSecurityMapping(this ModelBuilder modelBuilder)
        {
            new GroupMapping().Map(modelBuilder);
            new GroupPermissionMapping().Map(modelBuilder);
            new ModuleMapping().Map(modelBuilder);
            new ModulePermissionMapping().Map(modelBuilder);
            new GroupMapping().Map(modelBuilder);
            new UserGroupMapping().Map(modelBuilder);            
            new UserPermissionMapping().Map(modelBuilder);
            new UserTokenMapping().Map(modelBuilder);
            new DataProtectionElementMapping().Map(modelBuilder);
        }
    }
}