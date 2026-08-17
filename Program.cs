using AeonRegistryAPI.Endpoints.CustomIdentity;
using AeonRegistryAPI.Endpoints.Home;
using AeonRegistryAPI.Middleware;
using AeonRegistryAPI.Models;
using AeonRegistryAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCustomSwagger();

// Configure for Postgres
var connectionString = DataUtility.GetConnectionString(builder.Configuration);
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

// ASP.NET identity
// Assuming this is an internal app that doesn't require user to confirm their email.
builder.Services.AddIdentityApiEndpoints<ApplicationUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Admin policy
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

// Configure email
builder.Services.AddTransient<IEmailSender, ConsoleEmailService>();

// Enable validation for incoming DTOs
builder.Services.AddValidation();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    await DataSeed.ManageDataAsync(scope.ServiceProvider);
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // needed for images
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<BlockIdentityEndpoints>();

// Map API endpoints for login, logout, etc. using ASP.NET identity
var authRouteGroup = app.MapGroup("/api/auth")
    .WithTags("Admin");
authRouteGroup.MapIdentityApi<ApplicationUser>();

// Map custom endpoints
app.MapHomeEndpoints();
app.MapCustomIdentityEndpoints();

app.Run();