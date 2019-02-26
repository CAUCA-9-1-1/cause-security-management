using System;

namespace Cause.SecurityManagement.Models
{
	public class ModulePermission : BaseModel
	{
		public string Name { get; set; }
		public string Tag { get; set; }
		public Guid IdSystem { get; set; }
		public Module System { get; set; }
	}
}