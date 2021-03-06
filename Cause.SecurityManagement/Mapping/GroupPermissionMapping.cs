﻿using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cause.SecurityManagement.Mapping
{
	public class GroupPermissionMapping : BaseModelMapping<GroupPermission>
	{
		protected override void MapProperties(EntityTypeBuilder<GroupPermission> model)
		{
			model.HasOne(m => m.Permission)
				.WithMany()
				.HasForeignKey(m => m.IdModulePermission);
            model.HasOne(m => m.Group)
                .WithMany(m => m.Permissions)
                .HasForeignKey(m => m.IdGroup);
        }
	}
}