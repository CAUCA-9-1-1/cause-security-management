using System;

namespace Cause.SecurityManagement.Models
{
    public class UserDisconnectionLog : BaseModel
    {
		public Guid IdUser { get; set; }
		public string Description { get; set; }
		public DateTime DisconnectedOn { get; set; }
	}
}