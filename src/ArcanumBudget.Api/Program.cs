using System.Text;
using Going.Plaid;
using ArcanumBudget.Api.Data;
using ArcanumBudget.Api.Models;
using ArcanumBudget.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---- Database (SQL Server Express — free) ----
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---- ASP.NET Identity (free, built-in auth) ----
builder.Services
    .AddIdentity<AppUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// ---- JWT auth ----
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        };
    });

// ---- Data Protection (encrypts Plaid access tokens at rest) ----
builder.Services.AddDataProtection();

// ---- Plaid SDK ----
// Reads Plaid:ClientId / Plaid:Secret / Plaid:Environment from configuration.
builder.Services.AddPlaid(builder.Configuration);

// ---- App services ----
builder.Services.AddScoped<IPlaidService, PlaidService>();
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddScoped<IHouseholdService, HouseholdService>();
builder.Services.AddScoped<IRecommendationEngine, RecommendationEngine>();
builder.Services.AddScoped<IEmailService, ConsoleEmailService>(); // swap later for real email

// ---- CORS: allow the Ionic/Angular app (adjust origins for prod) ----
builder.Services.AddCors(options =>
{
    options.AddPolicy("AppClient", policy =>
    {
        policy.WithOrigins(
                "http://localhost:8100",  // ionic serve default
                "http://localhost:4200")  // ng serve default
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AppClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
