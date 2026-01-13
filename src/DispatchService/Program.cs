using DispatchService.Persistance;
using DispatchService.Services;
using DispatchService.Messaging.Consumers;
using DispatchService.Middlewares;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using System.Reflection;
using FluentValidation;
using MediatR;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type =>
        type.FullName!
            .Replace("+", ".")
    );
});

// DbContext (PostgreSQL)
builder.Services.AddDbContext<DispatchDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DispatchDatabase")
    )
);

// AWS Services (LocalStack)
builder.Services.AddLocalStackAws(builder.Configuration);

// Services
builder.Services.AddScoped<DispatchSvc>();

// Background Services
builder.Services.AddHostedService<IncidentEventConsumer>();
builder.Services.AddHostedService<IncidentUpdatedConsumer>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// MediatR
builder.Services.AddMediatR(cfg =>
   cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly())
);

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

var app = builder.Build();

// HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Global exception handling
app.UseMiddleware<ExceptionMiddleware>();

app.MapControllers();

app.UseHttpsRedirection();

app.Run();
