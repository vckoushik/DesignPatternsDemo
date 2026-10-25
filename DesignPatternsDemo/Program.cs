using System.Reflection;
using DesignPatternsDemo.Mapping;
using DesignPatternsDemo.Repository.InMemory;
using DesignPatternsDemo.Repository.Interfaces;
using DesignPatternsDemo.Services;
using DesignPatternsDemo.Services.Interfaces;
using Microsoft.OpenApi.Models;
using DesignPatternsDemo.Services.Patterns;
var builder = WebApplication.CreateBuilder(args);

// ── MVC / Controllers ────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── Swagger / OpenAPI ────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Design Patterns Demo API",
        Version     = "v1",
        Description = "A .NET 10 Web API playground for studying and comparing Software Design Patterns " +
                      "(Creational, Structural, Behavioral). Each pattern is implemented as an isolated service module.",
        Contact = new OpenApiContact
        {
            Name = "DesignPatternsDemo"
        }
    });

    // Include XML doc comments so endpoint <summary> tags appear in Swagger UI
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

    // Order endpoints alphabetically by path for a consistent layout
    options.OrderActionsBy(api => api.RelativePath);
});

// ── AutoMapper ───────────────────────────────────────────────────────────────
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile).Assembly));

// ── Application Services ─────────────────────────────────────────────────────
builder.Services.AddScoped<IProductService, ProductService>();

// ── Repositories ─────────────────────────────────────────────────────────────
// Singleton: in-memory store must survive the lifetime of the application.
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();

// -- Singleton Services ───────────────────────────────────────────────────────
builder.Services.AddSingleton<IConsoleLogger, ConsoleLogger>();

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    // Interactive Swagger UI at /swagger
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Design Patterns Demo API v1");
        options.RoutePrefix        = "swagger";         // Browse at http://localhost:5097/swagger
        options.DocumentTitle      = "DesignPatternsDemo — Swagger UI";
        options.DefaultModelsExpandDepth(1);            // Expand schemas one level by default
        options.DisplayRequestDuration();               // Show response time on each call
        options.EnableFilter();                         // Search/filter box across endpoints
        options.EnableTryItOutByDefault();              // Open "Try it out" on every endpoint
    });
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
