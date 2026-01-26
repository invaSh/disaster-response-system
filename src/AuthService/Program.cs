using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using AuthService.Domain;
using AuthService.Enums;
using AuthService.Helpers;
using AuthService.Middlewares;
using AuthService.Persistence;
using AuthService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AuthenticationDatabase")));

builder.Services.AddScoped<AuthService.Services.AuthSvc>();
builder.Services.AddScoped<JwtTokenGenerator>();
builder.Services.AddScoped<AuthService.Services.TokenSvc>();
builder.Services.AddScoped<AuthService.Services.UserSvc>();
builder.Services.AddScoped<AuthService.Services.RolesSvc>();


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            )
        };
    });

var app = builder.Build(); 

// migrimet per db automatikisht
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    dbContext.Database.Migrate();
    
    var rolesToSeed = new List<Role>
    {
        new Role { Id = Guid.NewGuid(), Name = "Admin", RoleType = RoleType.Admin },
        new Role { Id = Guid.NewGuid(), Name = "User", RoleType = RoleType.User },
        new Role { Id = Guid.NewGuid(), Name = "IncMan", RoleType = RoleType.IncMan },
        new Role { Id = Guid.NewGuid(), Name = "DisMan", RoleType = RoleType.DisMan }
    };
    
    foreach (var role in rolesToSeed)
    {
        if (!dbContext.Roles.Any(r => r.RoleType == role.RoleType))
        {
            dbContext.Roles.Add(role);
        }
    }
    
    dbContext.SaveChanges();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();

app.Run();
