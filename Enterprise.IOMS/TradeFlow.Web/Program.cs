using TradeFlow.Application;
using TradeFlow.Domain.Entities;
using TradeFlow.Infrastructure;
using TradeFlow.Infrastructure.Data;
using TradeFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using System.Threading.RateLimiting;
using TradeFlow.Shared.Constants;
using TradeFlow.Web.Components;
using TradeFlow.Web.Components.Account;
using TradeFlow.Web.Middleware;
using TradeFlow.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add MudBlazor
builder.Services.AddMudServices();

// Email service
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// Add Infrastructure (DbContext, Identity, Repository, TenantProvider, caching)
builder.Services.AddInfrastructure(builder.Configuration);

// Add Application Services (AutoMapper, all business services)
builder.Services.AddApplication();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// No FallbackPolicy here on purpose. A blanket RequireAuthenticatedUser fallback is also applied
// to the Blazor *endpoint* by the endpoint authorization conventions, which would make
// /Account/Login itself require authentication. The Identity cookie handler then challenges that
// request and redirects to /Account/Login?ReturnUrl=... forever.
//
// Pages are gated explicitly instead: every routable component under Components/Pages carries
// [Authorize] (admin pages use the Administrator policy), and Components/Account/Pages/Manage
// imports [Authorize] via its _Imports.razor. The anonymous /Account sign-in pages are left
// untouched.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.Administrator, policy => policy
        .RequireAuthenticatedUser()
        .RequireRole(Roles.Admin));

    options.AddPolicy(AuthorizationPolicies.Manager, policy => policy
        .RequireAuthenticatedUser()
        .RequireRole(Roles.Admin, Roles.Manager));
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Throttle credential endpoints so the Identity lockout counters are not the only defence
// against password spraying.
//
// The limiter is partitioned by client IP rather than left as a single global bucket: an
// unpartitioned fixed window of 10/minute would mean one attacker exhausts the budget and locks
// out every legitimate user behind the same gateway.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    var authPermitLimit = builder.Configuration.GetValue("RateLimiting:AuthPermitLimit", 10);

    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authPermitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var app = builder.Build();

// Behind IIS / Nginx / a cloud load balancer the app sees the proxy's scheme and IP,
// not the client's. Without this, UseHttpsRedirection 307-redirects the SignalR
// negotiate/post requests (killing the Blazor circuit with "connection closed" on the
// client) and the rate limiter partitions by the proxy's IP instead of the client's.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<GlobalExceptionLoggingMiddleware>();

app.UseRateLimiter();

app.UseAntiforgery();

app.MapStaticAssets();

var razorComponents = app.MapRazorComponents<App>();

// A Blazor EditForm posts back to its own route, so a sign-in attempt arrives at the
// /Account/Login *page* endpoint rather than at the Identity endpoint group. Attach the same
// limiter to those pages, otherwise the throttle would only cover external login and the page
// posts would slip past it.
razorComponents.Add(endpointBuilder =>
{
    if (endpointBuilder is RouteEndpointBuilder routeBuilder
        && routeBuilder.RoutePattern.RawText is { } route
        && route.StartsWith("/Account", StringComparison.OrdinalIgnoreCase))
    {
        endpointBuilder.Metadata.Add(new EnableRateLimitingAttribute("auth"));
    }
});

razorComponents.AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
