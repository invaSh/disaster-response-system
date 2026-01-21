using IncidentService.Persistance;
using Microsoft.EntityFrameworkCore;
using IncidentService.Services;
using IncidentService.Messaging.Publishers;
using IncidentService.Messaging.Consumers;
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
    // Schema customization for file uploads
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
    
    // Custom schema IDs
    c.CustomSchemaIds(type =>
        type.FullName!.Replace("+", "."));
    
    // API Info
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Incident Service API",
        Version = "v1"
    });
    
    // JWT Authentication
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
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

// Background Services (Consumers)
builder.Services.AddHostedService<DispatchEventConsumer>();

builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
