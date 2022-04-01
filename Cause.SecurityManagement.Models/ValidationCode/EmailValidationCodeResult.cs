using System;

namespace Cause.SecurityManagement.Models.ValidationCode
{
	public class EmailValidationCodeResult
	{
		public Guid UserId { get; set; }
		public string UserName { get; set; }
		public string Email { get; set; }
        public ValidationCodeType Type { get; set; }
	}
}
