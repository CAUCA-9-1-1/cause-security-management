using System;

namespace Cause.SecurityManagement.Core.Authentication.Exceptions;

public class DuplicateCertificateSubjectException : Exception
{
    public string CertificateSubjectDn { get; }

    public DuplicateCertificateSubjectException(string certificateSubjectDn)
        : base($"Multiple active certificate-bound external systems share the certificate subject DN '{certificateSubjectDn}'. Exactly one is required.")
    {
        CertificateSubjectDn = certificateSubjectDn;
    }
}
