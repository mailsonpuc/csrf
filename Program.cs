using System.Text.Json.Serialization;
using ApiCookieHttpOnly.Context;
using Microsoft.EntityFrameworkCore;
using NSwag;

var builder = WebApplication.CreateBuilder(args);

// Configura o DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers().AddJsonOptions(
    options => 
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);


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


// servico de jwt
builder.Services.AddAuthentication("Bearer").AddJwtBearer();



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

app.UseAuthorization();

app.MapControllers();

app.Run();