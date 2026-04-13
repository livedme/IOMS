using IOMS.Application;
using IOMS.Infrastructure;
using IOMS.Infrastructure.Identity;
using IOMS.Web.Components;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add MudBlazor
builder.Services.AddMudServices();

// Add Infrastructure (DbContext, Identity, Repository, TenantProvider)
builder.Services.AddInfrastructure(builder.Configuration);

// Add Application Services (AutoMapper, all business services)
builder.Services.AddApplication();

// Add Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Seed data
using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

//app.UseAuthentication();
//app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
