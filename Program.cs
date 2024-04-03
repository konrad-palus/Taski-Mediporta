using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using TaskApi_Mediporta.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Serilog config
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TestApi-Mediporta", Version = "v1" });
    c.MapType<SortByEnum>(() => new OpenApiSchema { Type = "string", Enum = Enum.GetNames(typeof(SortByEnum)).Select(name => new OpenApiString(name)).ToList<IOpenApiAny>() });
    c.MapType<SortOrderEnum>(() => new OpenApiSchema { Type = "string", Enum = Enum.GetNames(typeof(SortOrderEnum)).Select(name => new OpenApiString(name)).ToList<IOpenApiAny>() });

    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
});

builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddHostedService<TagImportHostedService>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();