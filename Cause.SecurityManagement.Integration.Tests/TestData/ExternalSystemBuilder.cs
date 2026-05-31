using Cause.SecurityManagement.Integration.Tests.Infrastructure;
using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Integration.Tests.TestData;

/// <summary>
/// Fluent builder for ExternalSystem test data.
/// </summary>
public class ExternalSystemBuilder
{
    private readonly TestSecurityContext _context;
    private string _name = $"system_{Guid.NewGuid():N}";
    private string _apiKey = Guid.NewGuid().ToString();
    private string? _certificateSubjectDn = null;
    private bool _isActive = true;

    public ExternalSystemBuilder(TestSecurityContext context) => _context = context;

    public ExternalSystemBuilder WithName(string name) { _name = name; return this; }
    public ExternalSystemBuilder WithApiKey(string apiKey) { _apiKey = apiKey; return this; }
    public ExternalSystemBuilder WithCertificateSubject(string dn) { _certificateSubjectDn = dn; return this; }
    public ExternalSystemBuilder IsInactive() { _isActive = false; return this; }

    public ExternalSystem Build()
    {
        var system = new ExternalSystem
        {
            Name = _name,
            ApiKey = _apiKey,
            CertificateSubjectDn = _certificateSubjectDn,
            IsActive = _isActive,
        };

        _context.ExternalSystems.Add(system);
        _context.SaveChanges();
        return system;
    }

    public string ApiKey => _apiKey;
}
