using System;

namespace Cause.SecurityManagement.Models
{
    public class UserToken : BaseToken
    {
		public Guid IdUser { get; set; }
	}
}