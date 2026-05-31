using System;

namespace Cause.SecurityManagement.Core.Authentication.Exceptions;

public class CertificateNotValidException : Exception
{
    public CertificateNotValidException(string message) : base(message)
    {
    }

    public CertificateNotValidException(string message, Exception inner) : base(message, inner)
    {
    }
}