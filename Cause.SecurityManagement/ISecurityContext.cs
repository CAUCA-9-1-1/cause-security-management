using System.Collections.Generic;
using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Cause.SecurityManagement
{
    public interface ISecurityContext<TUser>
        where TUser : User, new()
    {
		DbSet<TUser> Users { get; set; }
	    DbSet<UserGroup> UserGroups { get; set; }
	    DbSet<UserPermission> UserPermissions { get; set; }
	    DbSet<UserToken> UserTokens { get; set; }
        DbSet<UserValidationCode> UserValidationCodes { get; set; }
        DbSet<ExternalSystem> ExternalSystems { get; set; }
        DbSet<ExternalSystemToken> ExternalSystemTokens { get; set; }
        DbSet<Group> Groups { get; set; }
	    DbSet<GroupPermission> GroupPermissions { get; set; }
	    DbSet<Module> Modules { get; set; }
	    DbSet<ModulePermission> ModulePermissions { get; set; }
        DbSet<DataProtectionElement> DataProtectionXmlElements { get; set; }

        EntityEntry<TEntity> Add<TEntity>(TEntity entity) where TEntity : class;

		int SaveChanges();
    }
}
