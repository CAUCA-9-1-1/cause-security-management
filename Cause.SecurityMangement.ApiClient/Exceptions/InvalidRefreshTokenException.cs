using System;

namespace Cause.SecurityMangement.ApiClient.Exceptions
{
	public class InvalidRefreshTokenException : Exception
	{
		public InvalidRefreshTokenException() : base("The refresh token is invalid.")
		{
		}
	}
}