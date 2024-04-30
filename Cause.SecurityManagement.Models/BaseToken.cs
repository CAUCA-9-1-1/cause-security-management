using System;

namespace Cause.SecurityManagement.Models
{
    public abstract class BaseToken : BaseModel
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresOn { get; set; }
        public DateTime LoggedOn { get; set; } = DateTime.Now;
        public string ForIssuer { get; set; }
        public string Role { get; set; }
    }
}