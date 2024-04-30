using System;

namespace Cause.SecurityManagement.Services;

public class InvalidTokenException(string token, Exception innerException) 
    : Exception($"Token '{token} is invalid.", innerException);