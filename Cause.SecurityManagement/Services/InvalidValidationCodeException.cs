using System;

namespace Cause.SecurityManagement.Services
{
    public class InvalidValidationCodeException(string message) : Exception(message);
}