namespace Cauca.ApiClient
{
    public class LoginResult
    {
        public string AuthorizationType { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}