using Cause.Core.DataLayerExtensions.Mapping;
using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cause.SecurityManagement.Mapping
{
	public abstract class BaseModelMapping<T> : EntityMappingConfiguration<T>
		where T : BaseModel
	{
		public override void Map(EntityTypeBuilder<T> model)
		{
			model.HasKey(m => m.Id);
            model.Property(m => m.Id).ValueGeneratedNever();
			MapProperties(model);
		}

		protected abstract void MapProperties(EntityTypeBuilder<T> model);
	}
}