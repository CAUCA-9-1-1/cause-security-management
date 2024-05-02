using System.Collections.Generic;
using System.Linq;
using Cause.Core.DataLayerExtensions;
using Cause.SecurityManagement.Mapping;
using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Cause.SecurityManagement
{
    public abstract class BaseSecurityContext<TUser>(DbContextOptions options)
        : DbContext(options), ISecurityContext<TUser>
        where TUser : User, new()
    { 
        public DbSet<Module> Modules { get; set; }
		public DbSet<ModulePermission> ModulePermissions { get; set; }

		public DbSet<UserToken> UserTokens { get; set; }
        public DbSet<UserValidationCode> UserValidationCodes { get; set; }
        public DbSet<Group> Groups { get; set; }
		public DbSet<GroupPermission> GroupPermissions { get; set; }

		public DbSet<TUser> Users { get; set; }

		public DbSet<UserGroup> UserGroups { get; set; }
		public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<DataProtectionElement> DataProtectionXmlElements { get; set; }
        public DbSet<ExternalSystem> ExternalSystems { get; set; }
        public DbSet<ExternalSystemToken> ExternalSystemTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
            this.UseAutoDetectedMappings(modelBuilder);            
		}

        protected void AddSecurityManagementMappings(ModelBuilder modelBuilder)
        {
            new DataProtectionElementMapping().Map(modelBuilder);
            new GroupMapping().Map(modelBuilder);
            new GroupPermissionMapping().Map(modelBuilder);
            new ModuleMapping().Map(modelBuilder);
            new ModulePermissionMapping().Map(modelBuilder);
            new UserGroupMapping().Map(modelBuilder);
            new UserMapping<TUser>().Map(modelBuilder);
            new UserPermissionMapping().Map(modelBuilder);
            new UserValidationCodeMapping().Map(modelBuilder);
            new UserTokenMapping().Map(modelBuilder);
            new ExternalSystemMapping().Map(modelBuilder);
            new ExternalSystemTokenMapping().Map(modelBuilder);
        }

        public List<EntityEntry> GetModifieObjects()
        {
            return this.ChangeTracker.Entries().ToList();
        }
    }
}