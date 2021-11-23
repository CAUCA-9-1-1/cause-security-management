using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cause.SecurityManagement.Mapping
{
    public class UserValidationCodeMapping : BaseModelMapping<UserValidationCode>
    {
        protected override void MapProperties(EntityTypeBuilder<UserValidationCode> model)
        {
			model.Property(m => m.Code).HasMaxLength(100);
        }
    }
}