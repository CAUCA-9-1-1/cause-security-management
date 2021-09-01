using Cauca.ApiClient.Configuration;

namespace Cauca.ApiClient.Tests.Mocks
{
    public class MockConfiguration : IConfiguration
    {
        public string ApiBaseUrl { get; set; }
        public string ApiBaseUrlForAuthentication { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }
        public bool UseExternalSystemLogin { get; set; }
        public string AuthorizationType { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int RequestTimeoutInSeconds { get; set; } = 300;
    }
}
