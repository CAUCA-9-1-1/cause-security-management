using System;

namespace Cause.SecurityManagement.Models
{
    public class UserValidationCode : BaseModel
    {
        public Guid IdUser { get; set; }
		public string Code { get; set; }
		public DateTime ExpiresOn { get; set; }
		public ValidationCodeType Type { get; set; }
    }
}