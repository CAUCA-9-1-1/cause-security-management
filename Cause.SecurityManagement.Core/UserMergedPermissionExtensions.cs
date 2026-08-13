using System.Collections.Generic;
using Cause.SecurityManagement.Models.DataTransferObjects;

namespace Cause.SecurityManagement.Core
{
	internal static class UserMergedPermissionExtensions
	{
		public static bool Allows(this List<UserMergedPermission> permissions, string permissionTag)
			=> permissions.Exists(permission => permission.FeatureName == permissionTag && permission.Access);
	}
}
