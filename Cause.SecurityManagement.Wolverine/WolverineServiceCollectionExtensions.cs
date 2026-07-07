using Cause.SecurityManagement.Wolverine.ExternalSystem;
using Wolverine;

namespace Cause.SecurityManagement.Wolverine;

/// <summary>
/// Extension methods for registering Cause.SecurityManagement Wolverine message handlers and sagas.
/// </summary>
public static class WolverineServiceCollectionExtensions
{
    /// <summary>
    /// Registers Cause.SecurityManagement Wolverine handlers and sagas with the Wolverine bus,
    /// including the ExternalSystem authentication endpoints (logon/refresh).
    /// Call this inside your <c>UseWolverine</c> configuration action.
    /// </summary>
    /// <example>
    /// builder.Host.UseWolverine(opts =>
    /// {
    ///     opts.AddSecurityManagementHandlers();
    /// });
    /// </example>
    public static WolverineOptions AddSecurityManagementHandlers(this WolverineOptions opts)
    {
        opts.Discovery.IncludeAssembly(typeof(WolverineServiceCollectionExtensions).Assembly);
        opts.AddExternalSystemEndpoints();
        return opts;
    }
}
