using Microsoft.EntityFrameworkCore;
using NotificationService.Persistance;
using MediatR;
using NotificationService.Services;
using NotificationService.Messaging.Consumers;
using AutoMapper;
using FluentValidation;
using System.Reflection;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithAuth();

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("NotificationDatabase"))
);

// AWS Services (LocalStack)
builder.Services.AddLocalStackAws(builder.Configuration);

// Background Services
builder.Services.AddHostedService<IncidentEventConsumer>();
builder.Services.AddHostedService<IncidentUpdatedConsumer>();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

builder.Services.AddScoped<NotificationSvc>();

builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
