using System;

namespace Cause.SecurityManagement.Models
{
	public class ModulePermission : BaseModel
	{
		public string Name { get; set; }
		public string Tag { get; set; }
		public Guid IdModule { get; set; }
		public Module Module { get; set; }
	}
}