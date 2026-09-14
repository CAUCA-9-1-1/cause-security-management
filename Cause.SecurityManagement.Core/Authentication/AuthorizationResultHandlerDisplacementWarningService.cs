using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cause.SecurityManagement.Core.Authentication;

internal sealed record DisplacedAuthorizationResultHandlerWarning(string DisplacedTypeName);

/// <summary>
/// Warns at startup when AddAuthorizationRejectionLogging replaced an
/// IAuthorizationMiddlewareResultHandler the application had registered itself. A custom result
/// handler can make its own authorization decisions (for example denying some requests), so
/// silently displacing one can weaken the application's security posture; this makes that
/// displacement visible instead of leaving it a silent side effect of upgrading.
/// Never fails startup: displacement is (at worst) a behavior regression to investigate, not a
/// reason to prevent the application from starting.
/// </summary>
internal sealed class AuthorizationResultHandlerDisplacementWarningService(
    DisplacedAuthorizationResultHandlerWarning warning,
    ILogger<AuthorizationResultHandlerDisplacementWarningService> logger)
    : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "AddAuthorizationRejectionLogging replaced the previously registered " +
            "IAuthorizationMiddlewareResultHandler ({DisplacedResultHandlerType}) with " +
            "AuthorizationRejectionLogger. If {DisplacedResultHandlerType} made its own " +
            "authorization decisions, that behavior no longer runs. Pass " +
            "logAuthorizationRejections: false to the AddAuthorizationFor* extension (or call " +
            "AddAuthorizationRejectionLogging() before registering your own handler) to keep it.",
            warning.DisplacedTypeName, warning.DisplacedTypeName);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
