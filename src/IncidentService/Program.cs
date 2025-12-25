using IncidentService.Persistance;
using Microsoft.EntityFrameworkCore;
using IncidentService.Services;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using System.Reflection;
using FluentValidation;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<IncidentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("IncidentDatabase")));
builder.Services.AddScoped<IncidentSvc>();

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

app.MapControllers();

app.UseHttpsRedirection();

app.Run();
