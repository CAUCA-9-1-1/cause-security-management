using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cause.SecurityManagement.Mapping
{
	public class UserPermissionMapping : BaseModelMapping<UserPermission>
	{
		protected override void MapProperties(EntityTypeBuilder<UserPermission> model)
		{
			model.HasOne(m => m.Permission)
				.WithMany()
				.HasForeignKey(m => m.IdModulePermission)
                .OnDelete(DeleteBehavior.NoAction);
		}
	}
}