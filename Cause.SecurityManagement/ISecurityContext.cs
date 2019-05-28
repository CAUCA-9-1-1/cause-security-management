using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Cause.SecurityManagement
{
    public interface ISecurityContext
    {
		DbSet<User> Users { get; set; }
	    DbSet<UserGroup> UserGroups { get; set; }
	    DbSet<UserPermission> UserPermissions { get; set; }
	    DbSet<UserToken> UserTokens { get; set; }
	    DbSet<Group> Groups { get; set; }
	    DbSet<GroupPermission> GroupPermissions { get; set; }
	    DbSet<Module> Modules { get; set; }
	    DbSet<ModulePermission> ModulePermissions { get; set; }
        DbSet<DataProtectionElement> DataProtectionXMLElements { get; set; }

        EntityEntry<TEntity> Add<TEntity>(TEntity entity) where TEntity : class;
		int SaveChanges();
    }
}
