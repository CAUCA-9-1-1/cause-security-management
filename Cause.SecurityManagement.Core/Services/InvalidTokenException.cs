using System;

namespace Cause.SecurityManagement.Core.Services;

public class InvalidTokenException(string token, Exception innerException) 
    : Exception($"Token '{token} is invalid.", innerException);