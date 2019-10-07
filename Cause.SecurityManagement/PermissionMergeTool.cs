using System.Collections.Generic;
using System.Linq;
using Cause.SecurityManagement.Models.DataTransferObjects;

namespace Cause.SecurityManagement
{
	public class PermissionMergeTool
	{
		public List<UserMergedPermission> MergeUserAndGroupPermissions(List<UserMergedPermission> groupPermissions, List<UserMergedPermission> userPermissions)
		{
			return groupPermissions.Union(userPermissions)
				.GroupBy(permission => permission.FeatureName)
				.Select(CreateWebUserPermissionFromPermissionGroup).ToList();
		}

		protected UserMergedPermission CreateWebUserPermissionFromPermissionGroup(IGrouping<string, UserMergedPermission> group)
		{
			return new UserMergedPermission
			{
				FeatureName = group.Key,
				Access = group.All(p => p.Access)
			};
		}
	}
}
