using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cause.SecurityManagement.Mapping
{
    public class ExternalSystemTokenMapping : BaseModelMapping<ExternalSystemToken>
    {
        protected override void MapProperties(EntityTypeBuilder<ExternalSystemToken> model)
        {
            model.Property(m => m.AccessToken).HasMaxLength(500).IsRequired();
            model.Property(m => m.RefreshToken).HasMaxLength(100).IsRequired();
        }
    }
}