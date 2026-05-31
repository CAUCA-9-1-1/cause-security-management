using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cause.SecurityManagement.Core.Mapping
{
    public class DataProtectionElementMapping : BaseModelMapping<DataProtectionElement>
    {
        protected override void MapProperties(EntityTypeBuilder<DataProtectionElement> model)
        {
        }
    }
}