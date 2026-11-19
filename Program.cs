using System.Text.Json.Serialization;
using ApiCookieHttpOnly.Context;
using ApiCookieHttpOnly.Filters;
using ApiCookieHttpOnly.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NSwag;

var builder = WebApplication.CreateBuilder(args);

// Configura o DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers(options =>
    options.Filters.AddService<CookieAntiforgeryFilter>()).AddJsonOptions(
    options =>
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddScoped<CookieAntiforgeryFilter>();


builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "ApiCookieHttpOnly.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

// Configura o NSwag (OpenAPI + Swagger UI)
builder.Services.AddOpenApiDocument(options =>
{
    options.PostProcess = document =>
    {
        document.Info = new OpenApiInfo
        {
            Version = "v1",
            Title = "Catálogo API",
            Description = "API para gerenciamento de Categorias e Produtos",
            TermsOfService = "https://example.com/terms",
            Contact = new OpenApiContact
            {
                Name = "Example Contact",
                Url = "https://example.com/contact"
            },
            License = new OpenApiLicense
            {
                Name = "Example License",
                Url = "https://example.com/license"
            }
        };
    };

});

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

const string appCookieScheme = "AppCookie";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = appCookieScheme;
    options.DefaultChallengeScheme = appCookieScheme;
    options.DefaultSignInScheme = appCookieScheme;
}).AddCookie(appCookieScheme, options =>
{
    options.Cookie.Name = "ApiCookieHttpOnly.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(
        builder.Configuration.GetValue<double?>("Cookie:ExpirationInMinutes") ?? 60);
    options.SlidingExpiration = false;
});


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("SuperAdmin", policy => policy.RequireRole("User").RequireClaim("id", "mailson"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Gera o JSON em: /swagger/v1/swagger.json
    app.UseOpenApi();

    // Interface visual em: /swagger
    app.UseSwaggerUi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
