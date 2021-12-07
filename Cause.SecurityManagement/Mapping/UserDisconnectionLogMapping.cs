using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cause.SecurityManagement.Mapping
{
    public class UserDisconnectionLogMapping : BaseModelMapping<UserDisconnectionLog>
    {
        protected override void MapProperties(EntityTypeBuilder<UserDisconnectionLog> model)
        {
			model.Property(m => m.Description).HasMaxLength(500).IsRequired();
        }
    }
}