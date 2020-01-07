using System;

namespace Cause.SecurityMangement.ApiClient.Exceptions
{
	public class BadParameterApiException : ApiClientException
	{
		public BadParameterApiException(string url, Exception innerException = null) : base($"API returned a 400 (bad request) response for url '{url}'.", innerException)
		{
		}
	}
}