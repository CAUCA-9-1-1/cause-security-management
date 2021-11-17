using System;

namespace Cause.SecurityManagement.Models.DataTransferObjects
{
    public class UserPassword
	{
		public Guid Id { get; set; }
		public string Password { get; set; }
		public string PasswordConfirmation { get; set; }
	}
}