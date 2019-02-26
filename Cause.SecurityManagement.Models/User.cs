using System.Collections.Generic;

namespace Cause.SecurityManagement.Models
{
	public class User : BaseModel
	{		
		public string UserName { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Password { get; set; }
		public string Email { get; set; }

		public bool IsActive { get; set; }

		public ICollection<UserGroup> Groups { get; set; }
		public ICollection<UserPermission> Permissions { get; set; }
	}
}