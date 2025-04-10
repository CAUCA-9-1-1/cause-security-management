namespace Cause.SecurityManagement.Authentication;

public static class SecurityPolicy
{
    public const string Keycloak = "Keycloak";
    public const string RegularUser = "RegularUser";
    public const string KeycloakAndRegularUser = "KeycloakAndRegularUser";
    public const string UserRecovery = "UserRecoveryPolicy";
    public const string UserCreation = "UserCreationPolicy";
    public const string ApiCertificate = "ApiCertificatePolicy";
    public const string Metrics = "MetricsPolicy";
    public const string ExternalSystem = "ExternalSystemPolicy";
}