﻿namespace Cause.SecurityManagement.Models.Configuration
{
	public class SecurityConfiguration
    {
        public string Issuer { get; set; }
        public string PackageName { get; set; }
        public string SecretKey { get; set; }
        public string MinimalVersion { get; set; }
        public int? AccessTokenLifeTimeInMinutes { get; set; }
        public int? RefreshTokenLifeTimeInMinutes { get; set; }
        public int? TemporaryAccessTokenLifeTimeInMinutes { get; set; }
        public bool RefreshTokenCanExpire { get; set; } = true;
    }
}
