using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cause.SecurityManagement.Core.Authentication;

public sealed class AuthorizationRejectionLogger(ILogger<AuthorizationRejectionLogger> logger)
    : IAuthorizationMiddlewareResultHandler
{
    private const string DefaultSchemeLabel = "default";
    private const int MaxReasonLength = 512;
    private const string TruncationMarker = "...[truncated]";

    private readonly AuthorizationMiddlewareResultHandler innerHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        try
        {
            if (authorizeResult.Challenged)
                await TryLogRejectionReasonAsync(context, policy);
        }
        finally
        {
            // Delegation must happen on every path, including when logging itself faults -
            // an authorization decision must never be skipped because of a logging fault.
            await innerHandler.HandleAsync(next, context, policy, authorizeResult);
        }
    }

    private async Task TryLogRejectionReasonAsync(HttpContext context, AuthorizationPolicy policy)
    {
        try
        {
            var schemeResults = await CollectSchemeResultsAsync(context, policy);
            var schemeNames = string.Join(", ", schemeResults.Select(entry => entry.SchemeName));
            var reason = Truncate(DescribeReason(schemeResults));

            using var scope = logger.BeginScope("{RejectionScheme}", schemeNames);
            logger.LogInformation(
                "Request to {RequestPath} was rejected with 401. {RejectionReason}",
                context.Request.Path, reason);
        }
        catch (Exception exception)
        {
            LogWarningWithoutThrowing(context, exception);
        }
    }

    private void LogWarningWithoutThrowing(HttpContext context, Exception exception)
    {
        try
        {
            logger.LogWarning(exception,
                "Could not determine the reason for the 401 authorization challenge on {RequestPath}.",
                context.Request.Path);
        }
        catch
        {
            // Logging must never prevent delegation to the inner handler; see HandleAsync's finally.
        }
    }

    private static async Task<List<(string SchemeName, AuthenticateResult Result)>> CollectSchemeResultsAsync(
        HttpContext context, AuthorizationPolicy policy)
    {
        var results = new List<(string SchemeName, AuthenticateResult Result)>();

        if (policy.AuthenticationSchemes.Count > 0)
        {
            foreach (var scheme in policy.AuthenticationSchemes)
                results.Add((scheme, await context.AuthenticateAsync(scheme)));
        }
        else
        {
            var result = await context.AuthenticateAsync();
            results.Add((await ResolveDefaultSchemeNameAsync(context), result));
        }

        return results;
    }

    private static async Task<string> ResolveDefaultSchemeNameAsync(HttpContext context)
    {
        var schemeProvider = context.RequestServices.GetService<IAuthenticationSchemeProvider>();
        if (schemeProvider is null)
            return DefaultSchemeLabel;

        var scheme = await schemeProvider.GetDefaultAuthenticateSchemeAsync();
        return scheme?.Name ?? DefaultSchemeLabel;
    }

    private static string DescribeReason(IReadOnlyCollection<(string SchemeName, AuthenticateResult Result)> schemeResults)
    {
        var reasons = schemeResults
            .Where(entry => !entry.Result.Succeeded)
            .Select(entry => entry.Result.Failure is not null
                ? $"{entry.SchemeName}: {entry.Result.Failure.Message}"
                : $"{entry.SchemeName}: no credentials were presented")
            .ToList();

        // PolicyEvaluator only reaches HandleAsync with Challenged when at least one inspected
        // scheme did not succeed, so this branch is a defensive guard, not a reachable path: a
        // successful authenticate here would have produced Forbid, never Challenge.
        return reasons.Count > 0
            ? string.Join("; ", reasons)
            : "authorization denied without an authentication failure";
    }

    private static string Truncate(string reason)
    {
        if (reason.Length <= MaxReasonLength)
            return reason;

        var keep = Math.Max(MaxReasonLength - TruncationMarker.Length, 0);
        return string.Concat(reason.AsSpan(0, keep), TruncationMarker);
    }
}
