using DispatchService.Persistance;
using DispatchService.Services;
using DispatchService.Messaging.Consumers;
using DispatchService.Messaging.Publishers;
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

// Swagger with Authentication
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithAuth();

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

// Event Publisher
builder.Services.AddScoped<IDispatchEventPublisher, DispatchEventPublisher>();

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

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

// Apply database migrations automatically
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DispatchDbContext>();
    dbContext.Database.Migrate();
}

// HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Global exception handling
app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
