namespace TradeFlow.Web.Middleware;

/// <summary>
/// Adds hardening response headers to every request. Blazor Server renders the shell as HTML
/// and then communicates over a WebSocket, so the browser-side protections that matter here are
/// framing control, MIME sniffing control, and referrer/permission leakage reduction.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private static readonly string[] ScriptSources =
    [
        "'self'",
        "'unsafe-inline'",
        "blob:",
        "data:",
        "https://fonts.googleapis.com",
        "https://fonts.gstatic.com",
        "https://cdnjs.cloudflare.com"
    ];

    private static readonly string[] StyleSources =
    [
        "'self'",
        "'unsafe-inline'",
        "https://fonts.googleapis.com"
    ];

    private static readonly string[] ConnectSources =
    [
        "'self'",
        "ws:",
        "wss:",
        "https://fonts.googleapis.com",
        "https://fonts.gstatic.com"
    ];

    private static readonly string[] ImgSources =
    [
        "'self'",
        "data:",
        "blob:",
        "https:"
    ];

    private static readonly string[] FontSources =
    [
        "'self'",
        "data:",
        "https://fonts.gstatic.com"
    ];

    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(static state =>
        {
            var headers = ((HttpContext)state).Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["X-Permitted-Cross-Domain-Policies"] = "none";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            headers["Cross-Origin-Resource-Policy"] = "same-origin";
            headers["Permissions-Policy"] =
                "camera=(), microphone=(), geolocation=(), interest-cohort=()";

            // HSTS is only emitted over TLS; sending it on plaintext is a no-op at best.
            if (((HttpContext)state).Request.IsHttps)
                headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

            headers["Content-Security-Policy"] = BuildCsp();
            headers["Content-Security-Policy-Report-Only"] = BuildReportOnlyCsp();

            return Task.CompletedTask;
        }, context);

        return _next(context);
    }

    private static string BuildCsp() =>
        string.Join("; ", new[]
        {
            "default-src 'self'",
            $"script-src {string.Join(' ', ScriptSources)}",
            $"style-src {string.Join(' ', StyleSources)}",
            $"connect-src {string.Join(' ', ConnectSources)}",
            "font-src " + string.Join(' ', FontSources),
            "img-src " + string.Join(' ', ImgSources),
            "object-src 'none'",
            "base-uri 'self'",
            "form-action 'self'",
            "frame-ancestors 'none'",
            "manifest-src 'self'",
            "worker-src 'self' blob:"
        });

    // The reporting policy omits frame-ancestors (report-only silently ignores it) and relaxes
    // the script/style sources MudBlazor's runtime needs, so violations surface in reports
    // before the enforcing policy is tightened.
    //
    // font-src must be listed explicitly here. Without it font requests fall back to
    // default-src 'self', which rejects fonts.gstatic.com, and every page load reported a
    // violation for a font the enforcing policy was already allowing.
    private static string BuildReportOnlyCsp() =>
        string.Join("; ", new[]
        {
            "default-src 'self'",
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https: blob: data:",
            "style-src 'self' 'unsafe-inline' https:",
            "font-src 'self' data: https://fonts.gstatic.com",
            "connect-src 'self' ws: wss: https:",
            "img-src * data: blob:",
            "object-src 'none'",
            "base-uri 'self'"
        });
}
