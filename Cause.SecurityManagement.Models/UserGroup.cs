using System;

namespace Cause.SecurityManagement.Models
{
	public class UserGroup : BaseModel
	{
		public Guid IdUser { get; set; }
		public Guid IdGroup { get; set; }

		public User User { get; set; }
		public Group Group { get; set; }
	}
}