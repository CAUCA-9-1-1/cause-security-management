using System;

namespace Cause.SecurityManagement.Authentication.Exceptions
{
    public class CertificateNotValid : Exception
    {
        public CertificateNotValid()
        {
        }

        public CertificateNotValid(string message) : base(message)
        {
        }

        public CertificateNotValid(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
