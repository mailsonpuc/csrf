using System.Text;
using System.Text.Json.Serialization;
using ApiCookieHttpOnly.Context;
using ApiCookieHttpOnly.Filters;
using ApiCookieHttpOnly.Models;
using ApiCookieHttpOnly.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using NSwag.Generation.Processors.Security;

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

// Configura o NSwag (OpenAPI + Swagger UI) com suporte a JWT (cadeado)
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

    options.AddSecurity("Bearer", new OpenApiSecurityScheme
    {
        Type = OpenApiSecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = OpenApiSecurityApiKeyLocation.Header,
        Description = "Informe apenas o token JWT (não precisa digitar 'Bearer ' na frente)"
    });

    options.OperationProcessors.Add(
        new AspNetCoreOperationSecurityScopeProcessor("Bearer")
    );
});

builder.Services.AddScoped<ITokenService, TokenService>();

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Autenticação JWT
var secretKey = builder.Configuration["JWT:SecretKey"]
    ?? throw new ArgumentException("Invalid secret key!!");

const string appCookieScheme = "AppCookie";
const string smartAuthScheme = "SmartAuth";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = smartAuthScheme;
    options.DefaultChallengeScheme = smartAuthScheme;
    options.DefaultSignInScheme = appCookieScheme;
})
.AddPolicyScheme(smartAuthScheme, "Bearer ou cookie", options =>
{
    options.ForwardDefaultSelector = context =>
        context.Request.Headers.Authorization.ToString()
            .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? JwtBearerDefaults.AuthenticationScheme
            : appCookieScheme;
})
.AddCookie(appCookieScheme, options =>
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
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero,
        ValidAudience = builder.Configuration["JWT:ValidAudience"],
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey))
    };
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
