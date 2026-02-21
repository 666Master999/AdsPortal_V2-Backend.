// Program.cs
using AdsPortal_V2.Data;
using AdsPortal_V2.Helpers;
using AdsPortal_V2.Services;
using AdsPortal_V2.Middleware;
using AdsPortal_V2.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Uniform model validation responses for API controllers
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(kvp => kvp.Value?.Errors?.Count > 0)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray());

        return new BadRequestObjectResult(new { errors });
    };
});

// DbContext
builder.Services.AddDbContext<AdsPortalContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Jwt settings
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
                  ?? throw new InvalidOperationException("JWT configuration section is missing.");
if (string.IsNullOrWhiteSpace(jwtSettings.Key))
    throw new InvalidOperationException("JWT Key is not configured.");

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
// JWT helper
builder.Services.AddSingleton<IJwtService, JwtService>();

// Password hashing parameters
builder.Services.Configure<PasswordSettings>(builder.Configuration.GetSection("Password"));

// Forwarded headers toggle from config (useful when behind reverse proxy)
var useForwardedHeaders = builder.Configuration.GetValue<bool>("UseForwardedHeaders", true);
if (useForwardedHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(opts =>
    {
        opts.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
        // keep default known networks/proxies empty so ASP.NET uses the forwarded headers from any proxy
    });
}

// Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // We serve API over plain HTTP in this deployment, do not require HTTPS metadata
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// DI
builder.Services.AddScoped<IUserService, UserService>();

// CORS policies
builder.Services.AddCors(o =>
{
    // development Vite origin
    o.AddPolicy("AllowLocalhostVite", p => p.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod().AllowCredentials());

    // production frontend origin
    o.AddPolicy("FrontendCors", p => p.WithOrigins("https://adssite.somee.com")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

// Global exception middleware (first in pipeline)
app.UseMiddleware<ExceptionMiddleware>();

// Apply forwarded headers early if enabled
if (useForwardedHeaders)
{
    app.UseForwardedHeaders();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

app.UseRouting();

// Choose CORS policy depending on environment
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowLocalhostVite");
}
else
{
    app.UseCors("FrontendCors");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultControllerRoute();

app.Run();
