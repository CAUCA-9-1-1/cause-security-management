using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace Cause.SecurityManagement.Example
{
    public class UserCauca : User
    {
        public string SomeField { get; set; }
    }

    public class ExampleContext: DbContext, ISecurityContext<UserCauca>
    {
	    public DbSet<UserCauca> Users { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<UserToken> UserTokens { get; set; }
        public DbSet<ExternalSystem> ExternalSystems { get; set; }
        public DbSet<ExternalSystemToken> ExternalSystemTokens { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupPermission> GroupPermissions { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<ModulePermission> ModulePermissions { get; set; }
        public DbSet<DataProtectionElement> DataProtectionXmlElements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddSecurityMapping();
            base.OnModelCreating(modelBuilder);
        }
    }
}
