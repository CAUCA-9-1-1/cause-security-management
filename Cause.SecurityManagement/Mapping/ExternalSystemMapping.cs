using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cause.SecurityManagement.Mapping
{
    public class ExternalSystemMapping : BaseModelMapping<ExternalSystem>
    {
        protected override void MapProperties(EntityTypeBuilder<ExternalSystem> model)
        {
            model.Property(m => m.Name).HasMaxLength(50).IsRequired();
            model.Property(m => m.ApiKey).HasMaxLength(100).IsRequired();
            model.HasMany(m => m.Tokens)
                .WithOne()
                .HasForeignKey(m => m.IdExternalSystem);
        }
    }
}