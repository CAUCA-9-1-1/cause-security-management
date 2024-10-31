using System;

namespace Cause.SecurityManagement.Models.DataTransferObjects
{
    public class UserPassword
	{
		public Guid Id { get; set; }
		public string Password { get; set; }
		public string PasswordConfirmation { get; set; }
	}

    public class PasswordChangeRequest
    {
        public string NewPassword { get; set; }
    }

    public class AccountRecoveryRequest
    {
        public string Email { get; set; }
    }

    public class AccountRecoveryValidationRequest
    {
        public string Email { get; set; }
        public string ValidationCode { get; set; }
    }
}