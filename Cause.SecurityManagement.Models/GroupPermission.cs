using System;

namespace Cause.SecurityManagement.Models
{
	public class GroupPermission : BaseModel
	{
		public bool IsAllowed { get; set; }

		public Guid IdModulePermission { get; set; }
		public Guid IdGroup { get; set; }

		public ModulePermission Permission { get; set; }		
		public Group Group { get; set; }
	}
}