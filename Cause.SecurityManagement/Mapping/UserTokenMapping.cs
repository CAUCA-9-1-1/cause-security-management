using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cause.SecurityManagement.Mapping
{
	public class UserTokenMapping : BaseModelMapping<UserToken>
	{
		protected override void MapProperties(EntityTypeBuilder<UserToken> model)
		{
			model.Property(m => m.AccessToken).HasMaxLength(500).IsRequired();
			model.Property(m => m.RefreshToken).HasMaxLength(100).IsRequired();
		}
	}
}