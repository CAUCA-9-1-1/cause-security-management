using System;

namespace Cause.SecurityManagement.Models
{
	public class UserPermission : BaseModel
	{
		public bool IsAllowed { get; set; }

		public Guid IdSystemPermission { get; set; }
		public Guid IdUser { get; set; }

		public ModulePermission Permission { get; set; }
		public User User { get; set; }
	}
}