using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cause.SecurityManagement.Mapping
{
	public class UserMapping<TUser> : BaseModelMapping<TUser>
        where TUser: User, new()
	{
		protected override void MapProperties(EntityTypeBuilder<TUser> model)
		{
			model.Property(m => m.FirstName).HasMaxLength(100).IsRequired();
			model.Property(m => m.LastName).HasMaxLength(100).IsRequired();
			model.Property(m => m.UserName).HasMaxLength(100).IsRequired();
			model.Property(m => m.Password).HasMaxLength(100).IsRequired();
			model.Property(m => m.Email).HasMaxLength(100).IsRequired();
			model.HasMany(m => m.Groups)
				.WithOne()
				.HasForeignKey(m => m.IdUser);

			model.HasMany(m => m.Permissions)
				.WithOne()
				.HasForeignKey(m => m.IdUser);

		    model.HasMany(m => m.Tokens)
		        .WithOne()
		        .HasForeignKey(m => m.IdUser);
		}
	}
}