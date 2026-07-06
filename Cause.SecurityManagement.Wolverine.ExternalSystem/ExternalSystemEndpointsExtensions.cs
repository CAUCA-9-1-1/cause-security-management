using Wolverine;

namespace Cause.SecurityManagement.Wolverine.ExternalSystem;

/// <summary>
/// Extension methods for registering the Cause.SecurityManagement ExternalSystem Wolverine HTTP endpoints
/// (logon and refresh) without the rest of the Cause.SecurityManagement.Wolverine surface.
/// </summary>
public static class ExternalSystemEndpointsExtensions
{
    /// <summary>
    /// Registers only the ExternalSystem authentication endpoints (logon/refresh) with the Wolverine bus.
    /// Call this inside your <c>UseWolverine</c> configuration action.
    /// </summary>
    /// <example>
    /// builder.Host.UseWolverine(opts =>
    /// {
    ///     opts.AddExternalSystemEndpoints();
    /// });
    /// </example>
    public static WolverineOptions AddExternalSystemEndpoints(this WolverineOptions opts)
    {
        opts.Discovery.IncludeAssembly(typeof(ExternalSystemLogonEndpoint).Assembly);
        return opts;
    }
}
