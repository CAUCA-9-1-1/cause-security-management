using System;

namespace Cause.SecurityManagement.Services
{
    public class InvalidTokenException : Exception
    {
        public InvalidTokenException(string token, Exception innerException) 
            : base($"Token '{token} is invalid.", innerException)
        {
        }
    }
}
