using System;

namespace Cause.SecurityManagement.Core.Authentication.Exceptions
{
    public class ExternalSystemNotFound : Exception
    {
        public string CertificateSubjectDn { get; }

        public ExternalSystemNotFound()
        {
        }

        public ExternalSystemNotFound(string certificateSubjectDn)
            : base($"No active external system is registered for certificate subject DN '{certificateSubjectDn}'.")
        {
            CertificateSubjectDn = certificateSubjectDn;
        }
    }
}
