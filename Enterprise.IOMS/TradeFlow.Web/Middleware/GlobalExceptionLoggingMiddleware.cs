using System.Diagnostics;

namespace TradeFlow.Web.Middleware;

/// <summary>
/// Logs unhandled exceptions with full diagnostic context and returns a generic 500 so that
/// exception detail, stack traces, and connection strings never reach the client.
/// Previously a failed dashboard query was written to the console and swallowed, which made a
/// database outage indistinguishable from a genuinely empty dashboard.
/// </summary>
public sealed class GlobalExceptionLoggingMiddleware
{
    public const int UnhandledExceptionEventId = 1000;
    public const int SlowRequestEventId = 1001;

    private static readonly string[] SensitiveFragments =
    [
        "password=",
        "pwd=",
        "user id=",
        "uid=",
        "server=",
        "data source=",
        "initial catalog="
    ];

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionLoggingMiddleware> _logger;

    public GlobalExceptionLoggingMiddleware(RequestDelegate next, ILogger<GlobalExceptionLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var started = Stopwatch.GetTimestamp();

        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // A circuit disconnect or client navigation is not a fault; log it below Debug only.
            _logger.LogDebug(1002, "Request {Method} {Path} aborted by the client", context.Request.Method, context.Request.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                UnhandledExceptionEventId,
                ex,
                "Unhandled exception for {Method} {Path} (TraceId {TraceId}, Tenant {TenantId})",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier,
                ReadTenantId(context));

            if (!context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "An unexpected error occurred.",
                    traceId = context.TraceIdentifier
                });
            }

            return;
        }

        var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        if (elapsedMs > 1500)
        {
            _logger.LogWarning(
                SlowRequestEventId,
                "Slow request {Method} {Path} took {ElapsedMs:F0} ms",
                context.Request.Method,
                context.Request.Path,
                elapsedMs);
        }
    }

    private static string? ReadTenantId(HttpContext context) =>
        context.User?.FindFirst("TenantId")?.Value;

    public static string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var redacted = value;
        foreach (var fragment in SensitiveFragments)
        {
            var index = redacted.IndexOf(fragment, StringComparison.OrdinalIgnoreCase);
            while (index >= 0)
            {
                var end = redacted.IndexOf(';', index);
                if (end < 0)
                    end = redacted.Length;
                redacted = string.Concat(redacted.AsSpan(0, index), "***", redacted.AsSpan(end));
                index = redacted.IndexOf(fragment, StringComparison.OrdinalIgnoreCase);
            }
        }

        return redacted;
    }
}
