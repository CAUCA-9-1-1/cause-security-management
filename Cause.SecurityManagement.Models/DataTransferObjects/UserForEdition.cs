using System;

namespace Cause.SecurityManagement.Models.DataTransferObjects
{
    public class UserForEdition
    {
	    public Guid Id { get; set; } = Guid.NewGuid();
		public string UserName { get; set; } = "";
		public string FirstName { get; set; } = "";
		public string LastName { get; set; } = "";
		public string Email { get; set; } = "";
		public string Password { get; set; }
		public string PasswordConfirmation { get; set; }
	}
}
