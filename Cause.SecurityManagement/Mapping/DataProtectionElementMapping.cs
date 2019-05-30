using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cause.SecurityManagement.Mapping
{
    public class DataProtectionElementMapping : BaseModelMapping<DataProtectionElement>
    {
        protected override void MapProperties(EntityTypeBuilder<DataProtectionElement> model)
        {
        }
    }
}