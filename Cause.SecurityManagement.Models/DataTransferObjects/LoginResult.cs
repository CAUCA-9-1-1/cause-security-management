using System;

namespace Cause.SecurityManagement.Models.DataTransferObjects
{
	public class LoginResult
	{
		public string AuthorizationType { get; set; }
		public DateTime ExpiredOn { get; set; }
		public string AccessToken { get; set; }
		public string RefreshToken { get; set; }
		public bool MustVerifyCode { get; set; }
		public bool MustChangePassword { get; set; }
		public Guid IdUser { get; set; }
		public string Name { get; set; }
		public string Username { get; set; }
	}
}
