using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cause.SecurityManagement.Mapping
{
	public class ModuleMapping : BaseModelMapping<Module>
	{
		protected override void MapProperties(EntityTypeBuilder<Module> model)
		{
			model.Property(m => m.Name).HasMaxLength(100).IsRequired();
			model.HasMany(m => m.Permissions)
				.WithOne(m => m.Module)
				.HasForeignKey(m => m.IdModule)
                .OnDelete(DeleteBehavior.NoAction);
		}
	}
}