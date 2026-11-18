using System.Text.Encodings.Web;

namespace Avancer.License.Models
{

    public sealed class LicensePageModel
    {
        public required string Badge { get; init; }
        public required string Headline { get; init; }
        public required string Message { get; init; }
        public string? Domain { get; init; }
        public string? Error { get; init; }
        public string? Banner { get; init; }
        public string? LicenseKey { get; init; }
        public string? TrialEndsAt { get; init; }
        public bool CanActivate { get; init; }
        public bool IsAuthenticated { get; init; }
        public bool ShowContinueTrial { get; init; }
        public required string ReturnUrl { get; init; }
        public required string RequestToken { get; init; }
    }

    /// <summary>
    /// Renders the license gate as a complete HTML document in C#. There is deliberately no Razor
    /// component behind this page: it must render before Blazor starts and must keep working when the
    /// application itself cannot be licensed.
    /// </summary>
    public static class LicenseGatePageRenderer
    {
        private static readonly HtmlEncoder Encoder = HtmlEncoder.Default;

        public static string Render(LicensePageModel model)
        {
            var badge = Encode(model.Badge);
            var headline = Encode(model.Headline);
            var message = Encode(model.Message);
            var domain = Encode(model.Domain ?? string.Empty);
            var returnUrl = Encode(model.ReturnUrl);
            var requestToken = Encode(model.RequestToken);

            var error = string.IsNullOrWhiteSpace(model.Error)
                ? string.Empty
                : $"""<div class="alert" role="alert">{Encode(model.Error)}</div>""";

            var banner = string.IsNullOrWhiteSpace(model.Banner)
                ? string.Empty
                : $"""<div class="banner" role="alert">{Encode(model.Banner)}</div>""";

            var trialDetails = string.IsNullOrWhiteSpace(model.TrialEndsAt)
                ? string.Empty
                : $"""<div class="detail"><span>Trial ends</span><strong>{Encode(model.TrialEndsAt)}</strong></div>""";

            var continueTrial = model.ShowContinueTrial
                ? $"""
                <form method="post" action="/license/trial/continue" class="trial-form">
                    <input type="hidden" name="__RequestVerificationToken" value="{requestToken}">
                    <input type="hidden" name="returnUrl" value="{returnUrl}">
                    <button type="submit" class="secondary">Continue using the trial</button>
                </form>
                """
                : string.Empty;

            var activation = model.CanActivate
                ? BuildActivationForm(model.LicenseKey, returnUrl, requestToken, domain)
                : BuildSignInNote(model.IsAuthenticated, model.ReturnUrl);

            var activationBlock = model.ShowContinueTrial
                ? $"""
                <details class="activation-panel">
                    <summary>Activate a live license</summary>
                    {activation}
                </details>
                """
                : activation;

            return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <meta name="robots" content="noindex, nofollow">
                <title>{{headline}} - Avancer IOMS</title>
                <style>
                    :root {
                        color-scheme: light;
                        --ink: #14212b;
                        --muted: #5d6b78;
                        --line: #dbe3ea;
                        --accent: #166e63;
                        --accent-dark: #0f554c;
                        --danger: #a0342b;
                        --danger-bg: #fdf1ef;
                        --panel: #ffffff;
                        --canvas: #eef3f6;
                    }

                    * { box-sizing: border-box; }

                    body {
                        margin: 0;
                        min-height: 100vh;
                        display: grid;
                        place-items: center;
                        padding: 32px 20px;
                        background:
                            radial-gradient(circle at 15% 10%, rgba(22, 110, 99, .13), transparent 34rem),
                            radial-gradient(circle at 90% 90%, rgba(20, 33, 43, .10), transparent 30rem),
                            var(--canvas);
                        color: var(--ink);
                        font-family: "Segoe UI", system-ui, -apple-system, BlinkMacSystemFont, sans-serif;
                        line-height: 1.55;
                    }

                    main {
                        width: min(100%, 580px);
                        background: var(--panel);
                        border: 1px solid var(--line);
                        border-radius: 18px;
                        box-shadow: 0 24px 70px rgba(20, 33, 43, .13);
                        overflow: hidden;
                    }

                    header {
                        padding: 28px 32px 24px;
                        border-bottom: 1px solid var(--line);
                        background: linear-gradient(150deg, #f8fbfc, #eef5f4);
                    }

                    .brand {
                        display: flex;
                        align-items: center;
                        gap: 12px;
                        margin-bottom: 22px;
                        font-weight: 700;
                        letter-spacing: .01em;
                    }

                    .mark {
                        width: 38px;
                        height: 38px;
                        display: grid;
                        place-items: center;
                        border-radius: 11px;
                        background: var(--ink);
                        color: #fff;
                        font-size: 17px;
                        font-weight: 800;
                    }

                    .badge {
                        display: inline-flex;
                        align-items: center;
                        gap: 7px;
                        padding: 5px 11px;
                        border-radius: 999px;
                        background: #e5f1ef;
                        color: var(--accent-dark);
                        font-size: 12px;
                        font-weight: 700;
                        letter-spacing: .06em;
                        text-transform: uppercase;
                    }

                    h1 {
                        margin: 13px 0 8px;
                        font-size: clamp(24px, 5vw, 31px);
                        line-height: 1.16;
                        letter-spacing: -.025em;
                    }

                    .message { margin: 0; color: var(--muted); }

                    .body { padding: 26px 32px 30px; }

                    .detail {
                        display: flex;
                        justify-content: space-between;
                        gap: 16px;
                        padding: 11px 0;
                        border-bottom: 1px solid var(--line);
                        font-size: 14px;
                    }

                    .detail:first-child { padding-top: 0; }
                    .detail span { color: var(--muted); }
                    .detail strong { font-weight: 600; overflow-wrap: anywhere; text-align: right; }

                    .alert {
                        margin: 18px 0 20px;
                        padding: 12px 14px;
                        border: 1px solid #f0c5bf;
                        border-radius: 10px;
                        background: var(--danger-bg);
                        color: var(--danger);
                        font-size: 14px;
                    }

                    .banner {
                        margin-bottom: 18px;
                        padding: 13px 15px;
                        border-left: 4px solid var(--danger);
                        border-radius: 8px;
                        background: var(--danger-bg);
                        color: var(--danger);
                        font-size: 14px;
                        font-weight: 600;
                    }

                    label {
                        display: block;
                        margin: 20px 0 7px;
                        font-size: 13px;
                        font-weight: 700;
                        letter-spacing: .01em;
                    }

                    input[type="text"] {
                        width: 100%;
                        padding: 12px 13px;
                        border: 1px solid #b9c6d0;
                        border-radius: 10px;
                        color: var(--ink);
                        background: #fff;
                        font: inherit;
                        font-size: 14px;
                    }

                    input[type="text"]:focus {
                        outline: 3px solid rgba(22, 110, 99, .18);
                        border-color: var(--accent);
                    }

                    .hint { margin: 8px 0 0; color: var(--muted); font-size: 12.5px; }

                    button {
                        display: inline-flex;
                        align-items: center;
                        justify-content: center;
                        width: 100%;
                        margin-top: 18px;
                        padding: 12px 16px;
                        border: 0;
                        border-radius: 10px;
                        background: var(--accent);
                        color: #fff;
                        font: inherit;
                        font-size: 14px;
                        font-weight: 700;
                        cursor: pointer;
                        transition: background .15s ease;
                    }

                    button:hover { background: var(--accent-dark); }

                    button.secondary {
                        margin-top: 0;
                        background: #edf3f5;
                        color: var(--ink);
                    }

                    button.secondary:hover { background: #e3ebee; }

                    .trial-form { margin-bottom: 18px; }

                    .activation-panel {
                        margin-top: 4px;
                        border-top: 1px solid var(--line);
                        padding-top: 18px;
                    }

                    .activation-panel summary {
                        color: var(--accent-dark);
                        font-size: 14px;
                        font-weight: 700;
                        cursor: pointer;
                    }

                    .activation-panel summary + * { margin-top: 4px; }

                    .note {
                        margin: 18px 0 0;
                        padding: 13px 14px;
                        border-radius: 10px;
                        background: #f3f7f8;
                        color: var(--muted);
                        font-size: 13.5px;
                    }

                    .note a { color: var(--accent-dark); font-weight: 700; }

                    footer {
                        margin-top: 26px;
                        padding-top: 18px;
                        border-top: 1px solid var(--line);
                        color: #7b8894;
                        font-size: 12px;
                        text-align: center;
                    }

                    @media (max-width: 520px) {
                        header, .body { padding-left: 22px; padding-right: 22px; }
                    }
                </style>
            </head>
            <body>
                <main>
                    <header>
                        <div class="brand">
                            <span class="mark" aria-hidden="true">A</span>
                            <span>Avancer IOMS</span>
                        </div>
                        <span class="badge">{{badge}}</span>
                        <h1>{{headline}}</h1>
                        <p class="message">{{message}}</p>
                    </header>
                    <div class="body">
                        {{banner}}
                        {{trialDetails}}
                        {{error}}
                        {{continueTrial}}
                        {{activationBlock}}
                        <footer>License validation is handled by Avancer License Services.</footer>
                    </div>
                </main>
            </body>
            </html>
            """;
        }

        private static string BuildActivationForm(
            string? licenseKey,
            string encodedReturnUrl,
            string encodedRequestToken,
            string encodedDomain)
        {
            var key = Encode(licenseKey ?? string.Empty);
            var domainNote = encodedDomain.Length == 0
                ? string.Empty
                : $"""<p class="hint">The key is validated for <strong>{encodedDomain}</strong>.</p>""";

            return $"""
            <form method="post" action="/license/activate">
                <input type="hidden" name="__RequestVerificationToken" value="{encodedRequestToken}">
                <input type="hidden" name="returnUrl" value="{encodedReturnUrl}">
                <label for="licenseKey">License key</label>
                <input id="licenseKey" name="licenseKey" type="text" value="{key}"
                       autocomplete="off" autocapitalize="off" spellcheck="false"
                       maxlength="256" required>
                {domainNote}
                <button type="submit">Activate license</button>
            </form>
            """;
        }

        private static string BuildSignInNote(bool isAuthenticated, string returnUrl)
        {
            if (isAuthenticated)
            {
                return """
                <p class="note">
                    Only a workspace administrator can change the license key. Ask an administrator
                    to sign in and activate the license.
                </p>
                """;
            }

            var loginUrl = "/Account/Login?ReturnUrl=" + Uri.EscapeDataString(returnUrl);

            return $"""
            <p class="note">
                An administrator must sign in before the license can be activated.
                <a href="{Encode(loginUrl)}">Sign in</a>
            </p>
            """;
        }

        private static string Encode(string value) => Encoder.Encode(value);
    }
}