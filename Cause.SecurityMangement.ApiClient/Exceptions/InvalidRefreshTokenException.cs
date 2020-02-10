using System;

namespace Cauca.ApiClient.Exceptions
{
	public class InvalidRefreshTokenException : Exception
	{
		public InvalidRefreshTokenException() : base("The refresh token is invalid.")
		{
		}
	}
}