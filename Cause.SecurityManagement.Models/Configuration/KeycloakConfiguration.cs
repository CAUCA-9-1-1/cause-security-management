namespace Cause.SecurityManagement.Models.Configuration;

public class KeycloakConfiguration
{
    public static string Name = "KeycloakConfiguration";
    public string Url { get; set; }

    public string Realm { get; set; }

    public string ClientId { get; set; }

    public bool SslRequired { get; set; } = true;
    public string Resource { get; set; }
    public string AuthorizationUrl { get; set; }
    public string MetadataAddress { get; set; }
    public string ValidIssuer { get; set; }
    public string Audience => Resource;
    public bool ValidateSigningKey { get; set; } = true;
    public bool ShowDebugInfo { get; set; } = false;
}