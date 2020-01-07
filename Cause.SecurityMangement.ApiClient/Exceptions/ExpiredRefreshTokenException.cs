using System;

namespace Cause.SecurityMangement.ApiClient.Exceptions
{
	public class ExpiredRefreshTokenException : Exception
	{
		public ExpiredRefreshTokenException() : base("The refresh token is expired.")
		{
		}
	}
}