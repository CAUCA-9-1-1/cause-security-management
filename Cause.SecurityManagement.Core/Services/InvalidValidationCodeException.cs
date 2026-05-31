using System;

namespace Cause.SecurityManagement.Core.Services
{
    public class InvalidValidationCodeException(string message) : Exception(message);
}