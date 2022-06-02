using System;

namespace Cause.SecurityManagement.Models.ValidationCode
{
	public class BaseValidationCodeResult
	{
		public Guid UserId { get; set; }
		public string UserName { get; set; }
        public ValidationCodeType Type { get; set; }
	}
}
