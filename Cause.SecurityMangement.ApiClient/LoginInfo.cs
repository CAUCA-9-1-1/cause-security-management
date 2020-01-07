using System;

namespace Cause.SecurityMangement.ApiClient
{
    public class LoginInfo
    {
        public string AuthorizationType { get; set; } = "Bearer";
        public DateTime ExpiredOn { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}