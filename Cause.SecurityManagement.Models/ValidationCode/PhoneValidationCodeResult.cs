using System;

namespace Cause.SecurityManagement.Models.ValidationCode
{
	public class PhoneValidationCodeResult
	{
		public Guid UserId { get; set; }
		public string UserName { get; set; }
		public string PhoneNumber { get; set; }
        public ValidationCodeType Type { get; set; }
	}
}
