namespace Cauca.ApiClient.Configuration
{
    public interface IConfiguration
    {
        string ApiBaseUrl { get; set; }
        string ApiBaseUrlForAuthentication { get; set; }
        string UserId { get; set; }
        string Password { get; set; }
        bool UseExternalSystemLogin { get; set; }
        string AuthorizationType { get; set; }
        string AccessToken { get; set; }
        string RefreshToken { get; set; }
        int RequestTimeoutInSeconds { get; set; }
    }
}
