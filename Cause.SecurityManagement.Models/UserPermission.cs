using System;

namespace Cause.SecurityManagement.Models
{
	public class UserPermission : BaseModel
	{
		public bool IsAllowed { get; set; }

		public Guid IdModulePermission { get; set; }
		public Guid IdUser { get; set; }

		public ModulePermission Permission { get; set; }
	}
}