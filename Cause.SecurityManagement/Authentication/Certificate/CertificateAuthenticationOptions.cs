using Microsoft.AspNetCore.Authentication;

namespace Cause.SecurityManagement.Authentication.Certificate;

public class CertificateAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string Name = "CertificateScheme";
}