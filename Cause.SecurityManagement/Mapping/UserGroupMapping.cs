using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cause.SecurityManagement.Mapping
{
	public class UserGroupMapping : BaseModelMapping<UserGroup>
	{
		protected override void MapProperties(EntityTypeBuilder<UserGroup> model)
		{
			model.HasOne(m => m.Group)
				.WithMany(m => m.Users)
				.HasForeignKey(m => m.IdGroup)
                .OnDelete(DeleteBehavior.NoAction);
		}
	}
}