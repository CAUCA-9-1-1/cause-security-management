using System;

namespace Cauca.ApiClient.Exceptions
{
	public class ForbiddenApiException : ApiClientException
	{
		public ForbiddenApiException(string url, Exception innerException = null) : base($"API returned a 403 (forbidden) response for url '{url}'.", innerException)
		{
		}
	}
}