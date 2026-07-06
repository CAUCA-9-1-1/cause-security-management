namespace Cause.SecurityManagement.Core.Authentication;

public static class CustomAuthSchemes
{
    public const string RegularUserAuthentication = "RegularUserAuthentication";
    public const string KeycloakAuthentication = "KeycloakAuthentication";
    public const string RegularUserOrKeycloakScheme = "RegularUserOrKeycloakScheme";
    public const string RegularUserKeycloakOrConsoleScheme = "RegularUserKeycloakOrConsoleScheme";
    public const string CertificateAuthentication = "CertificateAuthentication";
    public const string ConsoleCertificateAuthentication = "ConsoleCertificateAuthentication";
    public const string ExternalSystemTokenAuthentication = "ExternalSystemTokenAuthentication";
    public const string DualExternalSystemScheme = "DualExternalSystemScheme";
}