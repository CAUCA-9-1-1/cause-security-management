using Cause.Core.DataLayerExtensions;
using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace Cause.SecurityManagement
{
	public abstract class BaseSecurityContext : DbContext, ISecurityContext
	{
		public DbSet<Module> Modules { get; set; }
		public DbSet<ModulePermission> ModulePermissions { get; set; }

		public DbSet<UserToken> UserTokens { get; set; }
		public DbSet<Group> Groups { get; set; }
		public DbSet<GroupPermission> GroupPermissions { get; set; }

		public DbSet<User> Users { get; set; }

		public DbSet<UserGroup> UserGroups { get; set; }
		public DbSet<UserPermission> UserPermissions { get; set; }

		protected BaseSecurityContext(DbContextOptions options) : base(options)
		{}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.AddTableNameToPrimaryKey();
			modelBuilder.UseAutoSnakeCaseMapping();
			modelBuilder.UseTablePrefix("tbl_");
			this.UseAutoDetectedMappings(modelBuilder);
		}
	}
}