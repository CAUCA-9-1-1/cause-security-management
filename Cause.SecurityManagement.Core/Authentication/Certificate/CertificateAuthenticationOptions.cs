using Microsoft.AspNetCore.Authentication;

namespace Cause.SecurityManagement.Core.Authentication.Certificate;

public class CertificateAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string Name = "CertificateScheme";
}