namespace Cause.SecurityManagement.Authentication;

public static class CustomAuthSchemes
{
    internal const string RegularUserAuthentication = "RegularUserAuthentication";
    internal const string KeycloakAuthentication = "KeycloakAuthentication";
    internal const string RegularUserOrKeycloakScheme = "RegularUserOrKeycloakScheme";
    internal const string RegularUserKeycloakOrConsoleScheme = "RegularUserKeycloakOrConsoleScheme";
    internal const string CertificateAuthentication = "CertificateAuthentication";
    internal const string ConsoleCertificateAuthentication = "ConsoleCertificateAuthentication";
}