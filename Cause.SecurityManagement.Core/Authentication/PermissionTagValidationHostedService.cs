using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cause.SecurityManagement.Core.Services.Management;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cause.SecurityManagement.Core.Authentication;

/// <summary>
/// Warns at startup when a permission attribute names a tag absent from the permission
/// catalog. Such a tag denies every RegularUser while Administrators still pass, which
/// presents as a permissions-data problem rather than a code defect.
/// Never fails startup: an unknown tag denies rather than grants, and the database may not
/// be migrated yet when this runs.
/// </summary>
internal sealed class PermissionTagValidationHostedService(
    IEnumerable<EndpointDataSource> endpointDataSources,
    IServiceScopeFactory scopeFactory,
    ILogger<PermissionTagValidationHostedService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var declaredTags = GetDeclaredTags();
        if (declaredTags.Count == 0)
            return;

        var knownTags = await GetKnownTagsAsync(cancellationToken);
        if (knownTags is null)
            return;

        foreach (var tag in declaredTags.Where(tag => !knownTags.Contains(tag)))
            logger.LogWarning(
                "Permission tag '{PermissionTag}' is used by an endpoint but is missing from the permission catalog. Every RegularUser will be denied.",
                tag);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private HashSet<string> GetDeclaredTags()
    {
        var tags = new HashSet<string>(StringComparer.Ordinal);
        foreach (var dataSource in endpointDataSources)
        {
            foreach (var endpoint in dataSource.Endpoints)
            {
                foreach (var authorizeData in endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>())
                {
                    if (PermissionPolicy.TryParse(authorizeData.Policy, out var tag, out _))
                        tags.Add(tag);
                }
            }
        }
        return tags;
    }

    private async Task<HashSet<string>> GetKnownTagsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var catalog = scope.ServiceProvider.GetService<IPermissionCatalogService>();
            if (catalog is null)
            {
                logger.LogWarning("Permission tag validation skipped: IPermissionCatalogService is not registered.");
                return null;
            }

            var permissions = await catalog.GetPermissionsAsync(cancellationToken);
            return [.. permissions.Select(permission => permission.Tag)];
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Permission tag validation skipped: the permission catalog could not be read.");
            return null;
        }
    }
}
