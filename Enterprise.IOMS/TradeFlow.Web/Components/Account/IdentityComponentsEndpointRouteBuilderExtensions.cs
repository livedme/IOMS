using TradeFlow.Domain.Entities;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;
using System.Text.Json;
using TradeFlow.Web.Components.Account.Pages;
using TradeFlow.Web.Components.Account.Pages.Manage;

namespace Microsoft.AspNetCore.Routing
{
    internal static class IdentityComponentsEndpointRouteBuilderExtensions
    {
        // These endpoints are required by the Identity Razor components defined in the /Components/Account/Pages directory of this project.
        public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
        {
            ArgumentNullException.ThrowIfNull(endpoints);

            var accountGroup = endpoints.MapGroup("/Account")
                .RequireRateLimiting("auth");

            accountGroup.MapPost("/PerformExternalLogin", (
                HttpContext context,
                [FromServices] SignInManager<ApplicationUser> signInManager,
                [FromForm] string provider,
                [FromForm] string returnUrl) =>
            {
                IEnumerable<KeyValuePair<string, StringValues>> query = [
                    new("ReturnUrl", returnUrl),
                    new("Action", ExternalLogin.LoginCallbackAction)];

                var redirectUrl = UriHelper.BuildRelative(
                    context.Request.PathBase,
                    "/Account/ExternalLogin",
                    QueryString.Create(query));

                var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
                return TypedResults.Challenge(properties, [provider]);
            });

            //accountGroup.MapPost("/Logout", async (
            //    ClaimsPrincipal user,
            //    [FromServices] SignInManager<ApplicationUser> signInManager,
            //    [FromForm] string returnUrl) =>
            //{
            //    await signInManager.SignOutAsync();
            //    return TypedResults.LocalRedirect($"~/{returnUrl}");
            //});

            accountGroup.MapPost("/Logout", async (
              ClaimsPrincipal user,
              [FromServices] SignInManager<ApplicationUser> signInManager,
              [FromForm] string? returnUrl) =>
            {
                await signInManager.SignOutAsync();
                var redirectTo = string.IsNullOrEmpty(returnUrl) ? "~/" : $"~/{returnUrl.TrimStart('/')}";
                return TypedResults.LocalRedirect(redirectTo);
            });

            // Registered as method groups rather than lambdas. The antiforgery failure path returns a
            // different TypedResults kind from the success paths, and C# has no way to annotate a
            // lambda's return type, so an inline lambda leaves the return type uninferable and MapPost
            // fails to resolve an overload.
            accountGroup.MapPost("/PasskeyCreationOptions", PasskeyCreationOptionsAsync);
            accountGroup.MapPost("/PasskeyRequestOptions", PasskeyRequestOptionsAsync);

            var manageGroup = accountGroup.MapGroup("/Manage").RequireAuthorization();

            manageGroup.MapPost("/LinkExternalLogin", async (
                HttpContext context,
                [FromServices] SignInManager<ApplicationUser> signInManager,
                [FromForm] string provider) =>
            {
                // Clear the existing external cookie to ensure a clean login process
                await context.SignOutAsync(IdentityConstants.ExternalScheme);

                var redirectUrl = UriHelper.BuildRelative(
                    context.Request.PathBase,
                    "/Account/Manage/ExternalLogins",
                    QueryString.Create("Action", ExternalLogins.LinkLoginCallbackAction));

                var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, signInManager.UserManager.GetUserId(context.User));
                return TypedResults.Challenge(properties, [provider]);
            });

            var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var downloadLogger = loggerFactory.CreateLogger("DownloadPersonalData");

            manageGroup.MapPost("/DownloadPersonalData", async (
                HttpContext context,
                [FromServices] UserManager<ApplicationUser> userManager,
                [FromServices] AuthenticationStateProvider authenticationStateProvider) =>
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user is null)
                {
                    return Results.NotFound($"Unable to load user with ID '{userManager.GetUserId(context.User)}'.");
                }

                var userId = await userManager.GetUserIdAsync(user);
                downloadLogger.LogInformation("User with ID '{UserId}' asked for their personal data.", userId);

                // Only include personal data for download
                var personalData = new Dictionary<string, string>();
                var personalDataProps = typeof(ApplicationUser).GetProperties().Where(
                    prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));
                foreach (var p in personalDataProps)
                {
                    personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "null");
                }

                var logins = await userManager.GetLoginsAsync(user);
                foreach (var l in logins)
                {
                    personalData.Add($"{l.LoginProvider} external login provider key", l.ProviderKey);
                }

                personalData.Add("Authenticator Key", (await userManager.GetAuthenticatorKeyAsync(user))!);
                var fileBytes = JsonSerializer.SerializeToUtf8Bytes(personalData);

                context.Response.Headers.TryAdd("Content-Disposition", "attachment; filename=PersonalData.json");
                return TypedResults.File(fileBytes, contentType: "application/json", fileDownloadName: "PersonalData.json");
            });

            return accountGroup;
        }

        private static async Task<IResult> PasskeyCreationOptionsAsync(
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromServices] IAntiforgery antiforgery)
        {
            if (!await TryValidateAntiforgeryAsync(context, antiforgery))
            {
                return TypedResults.BadRequest("The antiforgery token was missing or invalid.");
            }

            var user = await userManager.GetUserAsync(context.User);
            if (user is null)
            {
                return TypedResults.NotFound($"Unable to load user with ID '{userManager.GetUserId(context.User)}'.");
            }

            var userId = await userManager.GetUserIdAsync(user);
            var userName = await userManager.GetUserNameAsync(user) ?? "User";
            var optionsJson = await signInManager.MakePasskeyCreationOptionsAsync(new()
            {
                Id = userId,
                Name = userName,
                DisplayName = userName
            });

            return TypedResults.Content(optionsJson, contentType: "application/json");
        }

        private static async Task<IResult> PasskeyRequestOptionsAsync(
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromServices] IAntiforgery antiforgery,
            [FromQuery] string? username)
        {
            if (!await TryValidateAntiforgeryAsync(context, antiforgery))
            {
                return TypedResults.BadRequest("The antiforgery token was missing or invalid.");
            }

            // Conditional-mediation autofill runs the moment the sign-in page loads, before the user
            // has typed anything, so an absent username is the normal case rather than an error.
            // FindByNameAsync returns null for an unknown name, which MakePasskeyRequestOptionsAsync
            // accepts as "no allow credentials", so neither is dereferenced and neither can throw.
            var user = string.IsNullOrEmpty(username) ? null : await userManager.FindByNameAsync(username);
            var optionsJson = await signInManager.MakePasskeyRequestOptionsAsync(user);

            return TypedResults.Content(optionsJson, contentType: "application/json");
        }

        /// <summary>
        /// Validates the antiforgery token, reporting a failure as <c>false</c> rather than letting
        /// <see cref="AntiforgeryValidationException"/> escape.
        /// </summary>
        /// <remarks>
        /// These endpoints are called by <c>PasskeySubmit.razor.js</c> with fetch, outside the
        /// <c>UseAntiforgery</c> middleware's automatic validation, so the token is checked by hand.
        /// Unhandled, that exception propagated out of the endpoint and surfaced as a 500 with a
        /// generic "An unexpected error occurred" body — which reads as a server fault and writes a
        /// false stack trace to the log for what is really a stale or absent token. The passkey
        /// autofill in particular posts as soon as the sign-in page loads, so it is the path most
        /// likely to hit a token that no longer matches.
        /// </remarks>
        private static async Task<bool> TryValidateAntiforgeryAsync(HttpContext context, IAntiforgery antiforgery)
        {
            try
            {
                await antiforgery.ValidateRequestAsync(context);
                return true;
            }
            catch (AntiforgeryValidationException)
            {
                return false;
            }
        }
    }
}
