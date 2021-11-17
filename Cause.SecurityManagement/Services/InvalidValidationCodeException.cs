using System;

namespace Cause.SecurityManagement.Services
{
    public class InvalidValidationCodeException : Exception
    {
        public InvalidValidationCodeException(string message) : base (message)
        {
        }
    }
}