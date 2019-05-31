using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cause.SecurityManagement.Mapping
{
	public class ModulePermissionMapping : BaseModelMapping<ModulePermission>
	{
		protected override void MapProperties(EntityTypeBuilder<ModulePermission> model)
		{
			model.Property(m => m.Name).HasMaxLength(200).IsRequired();
			model.Property(m => m.Tag).HasMaxLength(100).IsRequired();
		}
	}
}