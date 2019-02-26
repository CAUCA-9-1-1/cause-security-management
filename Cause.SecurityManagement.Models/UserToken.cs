using System;

namespace Cause.SecurityManagement.Models
{
	public class UserToken : BaseModel
	{
		public string AccessToken { get; set; }
		public string RefreshToken { get; set; }
		public DateTime ExpiresOn { get; set; }

		public Guid IdUser { get; set; }

		public User User { get; set; }
	}
}