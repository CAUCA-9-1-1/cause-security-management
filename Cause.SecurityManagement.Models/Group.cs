using System.Collections.Generic;

namespace Cause.SecurityManagement.Models
{
	public class Group : BaseModel
	{
		public string Name { get; set; }

		public ICollection<GroupPermission> Permissions { get; set; }
	}
}