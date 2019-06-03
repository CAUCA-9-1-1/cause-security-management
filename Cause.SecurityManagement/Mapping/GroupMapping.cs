using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cause.SecurityManagement.Mapping
{
	public class GroupMapping : BaseModelMapping<Group>
	{
		protected override void MapProperties(EntityTypeBuilder<Group> model)
		{
			model.Property(m => m.Name).HasMaxLength(100).IsRequired();
            model.HasMany(m => m.Users)
                .WithOne(m => m.Group)
                .HasForeignKey(m => m.IdGroup);
        }
	}
}