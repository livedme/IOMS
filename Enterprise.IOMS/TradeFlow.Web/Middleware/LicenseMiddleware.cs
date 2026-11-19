using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Avancer.License.Models;
using Avancer.License.Wrapper;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TradeFlow.Infrastructure.Data;
using TradeFlow.Shared.Constants;
using TradeFlow.Web.Services;
using LicenseSubscriptionStatus = Avancer.License.Models.SubscriptionStatus;

namespace TradeFlow.Web.Middleware;

public sealed class LicenseMiddleware
{
    private const string ActivatePath = "/license/activate";
    private const string ActivateKeyPath = "/license/activate-key";
    private const string ContinueTrialPath = "/license/trial/continue";

    private readonly RequestDelegate _next;
    private readonly IOptions<LicenseSettings> _settings;
    private readonly ILicenseValidation _validation;
    private readonly IMemoryCache _cache;
    private readonly IAntiforgery _antiforgery;
    private readonly ILogger<LicenseMiddleware> _logger;

    public LicenseMiddleware(
        RequestDelegate next,
        IOptions<LicenseSettings> settings,
        ILicenseValidation validation,
        IMemoryCache cache,
        IAntiforgery antiforgery,
        ILogger<LicenseMiddleware> logger)
    {
        _next = next;
        _settings = settings;
        _validation = validation;
        _cache = cache;
        _antiforgery = antiforgery;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var settings = _settings.Value;

        if (!settings.Enabled)
        {
            await _next(context);
            return;
        }

        if (IsLicenseEndpoint(context.Request.Path))
        {
            try
            {
                await HandleLicenseEndpointAsync(context);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(exception, "License endpoint {Path} failed.", context.Request.Path);
                await WriteFallbackGateAsync(context);
            }

            return;
        }

        if (ShouldBypass(context.Request.Path))
        {
            await _next(context);
            return;
        }

        try
        {
            var domain = ResolveDomain(context, settings);
            if (domain.Equals("localhost"))
            {
                domain = "ims.avancerapp.com";
            }
            var licenseKey = await ResolveLicenseKeyAsync(context, settings, domain, context.RequestAborted);
            var trialUntil = ReadTrialExpiry(context);
            var trialExpired = trialUntil is { } expiry && expiry <= DateTime.UtcNow;

            LicenseCheck check;
            if (trialUntil is { } until && until > DateTime.UtcNow)
            {
                check = new LicenseCheck(CreateTrialResult(until, domain), false);
            }
            else
            {
                check = await GetCheckAsync(domain, licenseKey, context.RequestAborted);
            }

            WriteStatusCookie(context, check.Result);

            if (check.Refreshed && check.Result.Response is not null)
            {
                var tenantSynchronizer = context.RequestServices.GetRequiredService<LicenseTenantSynchronizer>();
                await tenantSynchronizer.SynchronizeAsync(check.Result, domain, licenseKey, context.RequestAborted);
            }

            if (!check.Result.IsLicenseValid)
            {
                await WriteGatePageAsync(context, check.Result, domain, licenseKey, null, trialExpired);
                return;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "License gate failed for {Method} {Path}.", context.Request.Method, context.Request.Path);
            await WriteFallbackGateAsync(context);
            return;
        }

        await _next(context);
    }

    private async Task<LicenseCheck> GetCheckAsync(string domain, string licenseKey, CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey(domain, licenseKey);

        if (_cache.TryGetValue(cacheKey, out LicenseSecurity? cached) && cached is not null)
            return new LicenseCheck(cached, false);

        LicenseSecurity result;
        try
        {
            result = await _validation.ValidateLicenseAsync(domain, licenseKey, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "License validation failed for domain {Domain}.", domain);
            result = new LicenseSecurity
            {
                IsLicenseValid = false,
                IsOffline = true,
                Message = "The license could not be validated. Please try again.",
                CheckedAtUtc = DateTime.UtcNow
            };
        }

        var lifetime = result.IsLicenseValid
            ? TimeSpan.FromMinutes(Math.Max(1, _settings.Value.CacheMinutes))
            : TimeSpan.FromMinutes(1);

        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = lifetime,
            Size = 1
        });

        return new LicenseCheck(result, true);
    }

    private async Task HandleLicenseEndpointAsync(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            return;
        }

        var settings = _settings.Value;
        var domain = ResolveDomain(context, settings);
        string? error = null;
        string? submittedKey = null;
        string? submittedReturnUrl = null;

        try
        {
            await _antiforgery.ValidateRequestAsync(context);

            var form = await context.Request.ReadFormAsync(context.RequestAborted);
            var returnUrl = SafeReturnUrl(form["returnUrl"].ToString());
            var cookieOptions = BuildCookieOptions(context, DateTimeOffset.UtcNow.AddDays(30));

            if (context.Request.Path.Equals(ActivatePath, StringComparison.OrdinalIgnoreCase))
            {
                var activateKey = form["licenseKey"].ToString().Trim();

                if (activateKey.Length == 0)
                {
                    error = "Enter a license key to continue.";
                }
                else
                {
                    var previousKey = await ResolveLicenseKeyAsync(context, settings, domain, context.RequestAborted);

                    var synchronizer = context.RequestServices.GetRequiredService<LicenseTenantSynchronizer>();
                    await synchronizer.PersistLicenseKeyAsync(activateKey, domain, context.RequestAborted);

                    context.Response.Cookies.Append(LicenseSettings.LicenseKeyCookie, activateKey, cookieOptions);
                    context.Response.Cookies.Delete(LicenseSettings.TrialCookie);
                    InvalidateCache(domain, previousKey, activateKey);
                    context.Response.Redirect(returnUrl);
                    return;
                }
            }
            else if (context.Request.Path.Equals(ActivateKeyPath, StringComparison.OrdinalIgnoreCase))
            {
                submittedKey = form["licenseKey"].ToString().Trim();
                submittedReturnUrl = returnUrl;

                if (submittedKey.Length == 0)
                {
                    error = "Enter a license key to continue.";
                }
                else
                {
                    var previousKey = await ResolveLicenseKeyAsync(context, settings, domain, context.RequestAborted);
                    var synchronizer = context.RequestServices.GetRequiredService<LicenseTenantSynchronizer>();

                    await synchronizer.PersistLicenseKeyAsync(submittedKey, domain, context.RequestAborted);

                    LicenseSecurity validation;
                    try
                    {
                        validation = await _validation.ValidateLicenseAsync(domain, submittedKey, context.RequestAborted);
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    {
                        _logger.LogError(exception, "License check failed for the submitted key on domain {Domain}.", domain);
                        validation = new LicenseSecurity
                        {
                            IsLicenseValid = false,
                            IsOffline = true,
                            Message = "The license check could not be completed. Please try again in a moment.",
                            CheckedAtUtc = DateTime.UtcNow
                        };
                    }

                    if (validation.Response is not null)
                    {
                        await synchronizer.SynchronizeAsync(validation, domain, submittedKey, context.RequestAborted);

                        context.Response.Cookies.Append(LicenseSettings.LicenseKeyCookie, submittedKey, cookieOptions);
                        context.Response.Cookies.Delete(LicenseSettings.TrialCookie);
                        InvalidateCache(domain, previousKey, submittedKey);
                        context.Response.Redirect(returnUrl);
                        return;
                    }

                    error = string.IsNullOrWhiteSpace(validation.Message)
                        ? "The license server did not return a response. Please try again."
                        : validation.Message;
                }
            }
            else if (ReadTrialExpiry(context) is not null)
            {
                error = "The trial period has ended. Activate a license to continue.";
            }
            else
            {
                var until = DateTime.UtcNow.AddDays(Math.Max(1, settings.TrialDays));

                context.Response.Cookies.Append(
                    LicenseSettings.TrialCookie,
                    until.ToString("O", CultureInfo.InvariantCulture),
                    BuildCookieOptions(context, until));

                InvalidateCache(domain, await ResolveLicenseKeyAsync(context, settings, domain, context.RequestAborted));
                context.Response.Redirect(returnUrl);
                return;
            }
        }
        catch (AntiforgeryValidationException)
        {
            error = "The activation form has expired. Please try again.";
        }

        var result = new LicenseSecurity
        {
            IsLicenseValid = false,
            Message = error ?? "The license could not be activated.",
            CheckedAtUtc = DateTime.UtcNow
        };

        if (context.Request.Path.Equals(ActivateKeyPath, StringComparison.OrdinalIgnoreCase))
        {
            await WriteFallbackGateAsync(context, error, submittedKey, submittedReturnUrl);
            return;
        }

        var trialExpired = ReadTrialExpiry(context) is not null;
        await WriteGatePageAsync(context, result, domain, await ResolveLicenseKeyAsync(context, settings, domain, context.RequestAborted), error, trialExpired);
    }

    private async Task WriteGatePageAsync(
        HttpContext context,
        LicenseSecurity result,
        string domain,
        string licenseKey,
        string? error,
        bool trialExpired)
    {
        if (context.Response.HasStarted)
            return;

        var tokens = _antiforgery.GetAndStoreTokens(context);
        var (headline, badge, banner) = Describe(result);
        var isAuthenticated = context.User.Identity?.IsAuthenticated == true;
        var expiry = result.Subscription?.ValidUntil is { } valid && valid != DateTime.MaxValue
            ? valid.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)
            : result.Subscription?.ExpiryDate is { } value && value != DateTime.MaxValue
                ? value.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)
                : null;

        var model = new LicensePageModel
        {
            Badge = badge,
            Headline = headline,
            Message = string.IsNullOrWhiteSpace(result.Message) ? banner : result.Message,
            Domain = domain,
            Error = error,
            Banner = banner,
            LicenseKey = licenseKey,
            TrialEndsAt = expiry,
            CanActivate = licenseKey.Length == 0 || isAuthenticated,
            IsAuthenticated = isAuthenticated,
            ShowContinueTrial = licenseKey.Length == 0 && !trialExpired,
            ReturnUrl = BuildReturnUrl(context),
            RequestToken = tokens.RequestToken ?? string.Empty
        };

        var html = LicenseGatePageRenderer.Render(model);

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(html, context.RequestAborted);
    }

    private async Task WriteFallbackGateAsync(
        HttpContext context,
        string? error = null,
        string? licenseKey = null,
        string? returnUrl = null)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/html; charset=utf-8";

        var tokens = _antiforgery.GetAndStoreTokens(context);
        var requestToken = WebUtility.HtmlEncode(tokens.RequestToken ?? string.Empty);
        var safeReturnUrl = WebUtility.HtmlEncode(SafeReturnUrl(returnUrl ?? BuildReturnUrl(context)));
        var key = WebUtility.HtmlEncode(licenseKey ?? string.Empty);
        var errorHtml = string.IsNullOrWhiteSpace(error)
            ? string.Empty
            : $"""<div class="alert" role="alert">{WebUtility.HtmlEncode(error)}</div>""";

        await context.Response.WriteAsync(
            $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <meta name="robots" content="noindex, nofollow">
                <title>License check unavailable - Avancer IOMS</title>
                <style>
                    :root { color-scheme: light; --ink: #14212b; --muted: #5d6b78; --line: #dbe3ea;
                            --accent: #166e63; --accent-dark: #0f554c; --danger: #a0342b;
                            --danger-bg: #fdf1ef; --canvas: #eef3f6; }
                    * { box-sizing: border-box; }
                    body { margin: 0; min-height: 100vh; display: grid; place-items: center;
                           padding: 24px; background: var(--canvas); color: var(--ink);
                           font-family: "Segoe UI", system-ui, sans-serif; }
                    main { width: min(100%, 520px); padding: 32px; background: #fff; border: 1px solid var(--line);
                           border-radius: 16px; box-shadow: 0 24px 70px rgba(20, 33, 43, .13); }
                    h1 { margin: 0 0 8px; font-size: 1.5rem; }
                    p { color: var(--muted); margin: 0; }
                    .alert { margin: 18px 0 0; padding: 12px 14px; border: 1px solid #f0c5bf;
                             border-radius: 10px; background: var(--danger-bg); color: var(--danger); font-size: 14px; }
                    label { display: block; margin: 22px 0 7px; font-size: 13px; font-weight: 700; }
                    input[type="text"] { width: 100%; padding: 12px 13px; border: 1px solid #b9c6d0;
                                         border-radius: 10px; color: var(--ink); font: inherit; font-size: 14px; }
                    input[type="text"]:focus { outline: 3px solid rgba(22, 110, 99, .18); border-color: var(--accent); }
                    button { display: inline-flex; align-items: center; justify-content: center; width: 100%;
                             margin-top: 18px; padding: 12px 16px; border: 0; border-radius: 10px;
                             background: var(--accent); color: #fff; font: inherit; font-size: 14px;
                             font-weight: 700; cursor: pointer; }
                    button:hover { background: var(--accent-dark); }
                </style>
            </head>
            <body>
                <main>
                    <h1>License check unavailable</h1>
                    <p>The license check could not be completed. Please try again in a moment.</p>
                    {{errorHtml}}
                    <form method="post" action="/license/activate-key">
                        <input type="hidden" name="__RequestVerificationToken" value="{{requestToken}}">
                        <input type="hidden" name="returnUrl" value="{{safeReturnUrl}}">
                        <label for="licenseKey">License key</label>
                        <input id="licenseKey" name="licenseKey" type="text" value="{{key}}"
                               autocomplete="off" autocapitalize="off" spellcheck="false"
                               maxlength="256" required>
                        <button type="submit">Activate license</button>
                    </form>
                </main>
            </body>
            </html>
            """,
            context.RequestAborted);
    }

    private static (string Headline, string Badge, string Banner) Describe(LicenseSecurity result)
    {
        var status = result.Subscription?.Status;

        if (status == LicenseSubscriptionStatus.Expired)
        {
            var expiry = result.Subscription?.ValidUntil is { } valid && valid != DateTime.MaxValue
                ? valid.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)
                : result.Subscription?.ExpiryDate is { } value && value != DateTime.MaxValue
                    ? value.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)
                    : "the renewal date";

            return (
                "License expired",
                "Renewal required",
                $"Your subscription expired on {expiry}. Renew the Avancer license to keep using the application.");
        }

        if (status is LicenseSubscriptionStatus.Inactive
            or LicenseSubscriptionStatus.Suspended
            or LicenseSubscriptionStatus.Revoked
            or LicenseSubscriptionStatus.Cancelled
            or LicenseSubscriptionStatus.Pending
            or LicenseSubscriptionStatus.PastDue)
        {
            return (
                "License inactive",
                "Action required",
                $"Your license status is {status}. Contact your administrator or the Avancer support team to restore access.");
        }

        if (result.IsOffline)
        {
            return (
                "License service unavailable",
                "Connection issue",
                "The Avancer license service could not be reached. Access will resume automatically once it is available again.");
        }

        return (
            "License invalid",
            "Action required",
            "The configured license key is not valid for this domain. Enter a valid key to continue.");
    }

    private void WriteStatusCookie(HttpContext context, LicenseSecurity result)
    {
        try
        {
            WriteStatusCookieCore(context, result);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogDebug(exception, "Could not write the license status cookie.");
        }
    }

    private static void WriteStatusCookieCore(HttpContext context, LicenseSecurity result)
    {
        var payload = new LicenseStatusPayload(
            result.IsLicenseValid,
            result.IsOffline,
            result.Subscription?.Status.ToString() ?? "Unknown",
            result.Subscription?.Plan,
            result.Subscription?.BillingCycle?.ToString(),
            result.Subscription?.ValidUntil,
            result.Subscription?.ExpiryDate,
            result.Subscription?.GracePeriod,
            result.Message,
            result.CheckedAtUtc);

        var json = JsonSerializer.Serialize(payload);
        var value = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

        context.Response.Cookies.Append(
            LicenseSettings.StatusCookie,
            value,
            BuildCookieOptions(context, DateTimeOffset.UtcNow.AddDays(1)));
    }

    private static string ResolveDomain(HttpContext context, LicenseSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.Domain))
            return settings.Domain.Trim();

        var host = context.Request.Host.Host;
        return string.IsNullOrWhiteSpace(host) ? KeySetting.DefaultDomain : host;
    }

    private async Task<string> ResolveLicenseKeyAsync(
        HttpContext context,
        LicenseSettings settings,
        string domain,
        CancellationToken cancellationToken)
    {
        var fromTenant = await TryGetTenantLicenseKeyAsync(context, domain, cancellationToken);
        if (!string.IsNullOrWhiteSpace(fromTenant))
            return fromTenant.Trim();

        var fromCookie = ReadCookie(context, LicenseSettings.LicenseKeyCookie);
        if (!string.IsNullOrWhiteSpace(fromCookie))
            return fromCookie.Trim();

        return settings.LicenseKey?.Trim() ?? string.Empty;
    }

    private static async Task<string?> TryGetTenantLicenseKeyAsync(
        HttpContext context,
        string domain,
        CancellationToken cancellationToken)
    {
        try
        {
            var dbFactory = context.RequestServices.GetService<IDbContextFactory<ApplicationDbContext>>();
            if (dbFactory is null)
                return null;

            var tenantProvider = context.RequestServices.GetService<ITenantProvider>();
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            Guid? tenantId = tenantProvider?.TryGetTenantId();

            if (tenantId is { } resolvedId && resolvedId != Guid.Empty)
            {
                var byId = await db.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == resolvedId, cancellationToken);

                if (!string.IsNullOrWhiteSpace(byId?.LicenseKey))
                    return byId.LicenseKey;
            }

            if (!string.IsNullOrWhiteSpace(domain))
            {
                var candidate = domain.Trim();

                var byDomain = await db.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        t => (t.ApplicationDomain != null && t.ApplicationDomain == candidate), cancellationToken);

                if (!string.IsNullOrWhiteSpace(byDomain?.LicenseKey))
                    return byDomain.LicenseKey;
            }

            //var defaultTenant = await db.Tenants
            //    .AsNoTracking()
            //    .FirstOrDefaultAsync(t => t.Id == Guid.Parse(AppConstants.DefaultTenantId), cancellationToken);
            //return defaultTenant?.LicenseKey;

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string? ReadCookie(HttpContext context, string name)
    {
        try
        {
            return context.Request.Cookies[name];
        }
        catch
        {
            return null;
        }
    }

    private static DateTime? ReadTrialExpiry(HttpContext context)
    {
        var value = ReadCookie(context, LicenseSettings.TrialCookie);
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var until)
            ? until
            : null;
    }

    private static LicenseSecurity CreateTrialResult(DateTime until, string domain) => new()
    {
        IsLicenseValid = true,
        Message = "The trial license is active.",
        CheckedAtUtc = DateTime.UtcNow,
        Subscription = new Subscription
        {
            DomainName = domain,
            Plan = "Trial",
            Status = LicenseSubscriptionStatus.Trial,
            ActiveDate = DateTime.UtcNow,
            ValidUntil = until,
            ExpiryDate = until
        }
    };

    private string BuildReturnUrl(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";

        if (path.StartsWith("/license/", StringComparison.OrdinalIgnoreCase))
            return "/";

        return string.Concat(path, context.Request.QueryString.Value);
    }

    private static string SafeReturnUrl(string returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) ||
            !returnUrl.StartsWith('/') ||
            returnUrl.StartsWith("//") ||
            returnUrl.StartsWith("/license/", StringComparison.OrdinalIgnoreCase))
        {
            return "/";
        }

        return returnUrl;
    }

    private static bool IsLicenseEndpoint(PathString path) =>
        path.Equals(ActivatePath, StringComparison.OrdinalIgnoreCase) ||
        path.Equals(ActivateKeyPath, StringComparison.OrdinalIgnoreCase) ||
        path.Equals(ContinueTrialPath, StringComparison.OrdinalIgnoreCase);

    private static bool ShouldBypass(PathString path)
    {
        var value = path.Value ?? string.Empty;

        if (value.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("/_content", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("/Account/", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("/not-found", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("/Error", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("/favicon.png", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return Path.GetExtension(value) is
            ".css" or ".js" or ".map" or ".png" or ".jpg" or ".jpeg" or
            ".gif" or ".svg" or ".ico" or ".woff" or ".woff2" or ".ttf";
    }

    private static CookieOptions BuildCookieOptions(HttpContext context, DateTimeOffset expires) => new()
    {
        HttpOnly = true,
        Secure = context.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        Expires = expires
    };

    private string BuildCacheKey(string domain, string licenseKey) =>
        $"license:gate:{domain}:{licenseKey}";

    private void InvalidateCache(string domain, params string[] licenseKeys)
    {
        foreach (var key in licenseKeys.Where(key => !string.IsNullOrWhiteSpace(key)).Distinct())
            _cache.Remove(BuildCacheKey(domain, key));
    }

    private sealed record LicenseCheck(LicenseSecurity Result, bool Refreshed);

    private sealed record LicenseStatusPayload(
        bool Valid,
        bool Offline,
        string Status,
        string? Plan,
        string? BillingCycle,
        DateTime? ValidUntil,
        DateTime? ExpiryDate,
        int? GracePeriod,
        string Message,
        DateTime CheckedAtUtc);
}
