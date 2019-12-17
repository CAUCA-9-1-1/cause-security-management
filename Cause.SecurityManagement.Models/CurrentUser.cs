using System;

namespace Cause.SecurityManagement.Models
{
	public class CurrentUser
	{
		public Guid Id { get; set; }
		public string UserName { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
	}
}
