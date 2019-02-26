using System;

namespace Cause.SecurityManagement.Models.DataTransferObjects
{
	public class LoginResult
	{
		public string AuthorizationType { get; set; } = "Bearer";
		public DateTime ExpiredOn { get; set; }
		public string AccessToken { get; set; }
		public string RefreshToken { get; set; }
		public Guid IdUser { get; set; }
		public string Name { get; set; }
	}
}
