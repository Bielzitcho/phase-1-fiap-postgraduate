using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OficinaTech.Application.Interfaces;
using OficinaTech.Application.Mapping;
using OficinaTech.Application.Services;
using OficinaTech.Domain.Repositories;
using OficinaTech.Infrastructure.Data;
using OficinaTech.Infrastructure.Repositories;
using OficinaTech.Infrastructure.Services;
using OficinaTech.Presentation.Middleware;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// OpenAPI / Scalar (INFRA-01)
builder.Services.AddOpenApi();

// EF Core + Npgsql (D-08)
builder.Services.AddDbContext<OficinaTechDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repository DI registrations
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IServiceTypeRepository, ServiceTypeRepository>();
builder.Services.AddScoped<IPartRepository, PartRepository>();
builder.Services.AddScoped<IServiceOrderRepository, ServiceOrderRepository>();
builder.Services.AddScoped<AdminCredentialService>();

// Unit of Work
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();

// Application services
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IServiceTypeService, ServiceTypeService>();
builder.Services.AddScoped<IPartService, PartService>();

// Global exception handler (D-05, D-06)
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();

// JWT Authentication (AUTH-01, AUTH-02)
var jwtSecret = builder.Configuration["Admin:JwtSecret"]
    ?? throw new InvalidOperationException("Admin:JwtSecret is required.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Mapster central mapping config — called before Build() (D-04)
MappingConfig.Register();

var app = builder.Build();

// Auto-migration on startup (D-08)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OficinaTechDbContext>();
    await db.Database.MigrateAsync();
}

// Middleware pipeline — ORDER IS CRITICAL (Pitfall 5)
app.UseExceptionHandler();   // FIRST — outermost catcher for DomainException

app.MapOpenApi();
app.MapScalarApiReference(); // INFRA-01: NOT inside IsDevelopment() — reachable in all environments

app.UseAuthentication();     // MUST come before UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();
