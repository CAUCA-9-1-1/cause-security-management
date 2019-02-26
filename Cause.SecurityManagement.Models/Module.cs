using System.Collections.Generic;

namespace Cause.SecurityManagement.Models
{
	public class Module : BaseModel
	{
		public string Name { get; set; }
		public string Tag { get; set; }

		public ICollection<ModulePermission> Permissions { get; set; }
	}
}