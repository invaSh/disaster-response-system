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
using IncidentService.Helpers; // Kept from HEAD
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
    
    c.CustomSchemaIds(type => type.FullName!.Replace("+", "."));
    
    c.SchemaFilter<EnumSchemaFilter>();
    
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Incident Service API",
        Version = "v1"
    });
    
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer 12345abcdef\"",
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

builder.Services.AddLocalStackAws(builder.Configuration);
builder.Services.AddScoped<IS3Service, S3Service>();
builder.Services.AddScoped<IIncidentEventPublisher, IncidentEventPublisher>();
builder.Services.AddHostedService<DispatchEventConsumer>();

builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IncidentDbContext>();
    dbContext.Database.Migrate();
}

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
