namespace Cauca.ApiClient.Exceptions
{
	public class UnauthorizedApiException : ApiClientException
	{
		public UnauthorizedApiException(string url) : base($"API returned a 401 (unauthorized) response for url '{url}'.")
		{
		}
	}
}