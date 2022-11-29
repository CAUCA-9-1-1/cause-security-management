using Cause.Core.DataLayerExtensions.Mapping;
using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cause.SecurityManagement.Mapping
{
	public abstract class BaseModelMapping<T> : EntityMappingConfiguration<T>
		where T : BaseModel
	{
		public override void Map(EntityTypeBuilder<T> builder)
		{
			builder.HasKey(m => m.Id);
            builder.Property(m => m.Id).ValueGeneratedNever();
			MapProperties(builder);
		}

		protected abstract void MapProperties(EntityTypeBuilder<T> model);
	}
}