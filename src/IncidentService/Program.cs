using IncidentService.Persistance;
using Microsoft.EntityFrameworkCore;
using IncidentService.Services;
using IncidentService.Messaging.Publishers;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using System.Reflection;
using FluentValidation;
using IncidentService.Middlewares;
using Shared.Extensions;
using Microsoft.AspNetCore.Http;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type =>
        type.FullName!
            .Replace("+", ".")
    );
    
    c.MapType<IFormFile>(() => new Microsoft.OpenApi.Models.OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });
    
    c.MapType<List<IFormFile>>(() => new Microsoft.OpenApi.Models.OpenApiSchema
    {
        Type = "array",
        Items = new Microsoft.OpenApi.Models.OpenApiSchema
        {
            Type = "string",
            Format = "binary"
        }
    });
    
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Incident Service API",
        Version = "v1"
    });
});

builder.Services.AddDbContext<IncidentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("IncidentDatabase")));
builder.Services.AddScoped<IncidentSvc>();

// AWS Services (LocalStack)
builder.Services.AddLocalStackAws(builder.Configuration);

// S3 Service
builder.Services.AddScoped<IS3Service, S3Service>();

// Event Publisher
builder.Services.AddScoped<IIncidentEventPublisher, IncidentEventPublisher>();

builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();

app.MapControllers();

app.UseHttpsRedirection();

app.Run();
