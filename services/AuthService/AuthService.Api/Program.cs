using System.Text;
using AuthService.Application.Services;
using AuthService.Application.Validators;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Data;
using AuthService.Infrastructure.Messaging;
using AuthService.Infrastructure.Outbox;
using AuthService.Infrastructure.Repositories;
using AuthService.Infrastructure.Security;
using AuthService.Api.Middleware;
using Common.Monitoring;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

// HttpContextAccessor нужен для получения OutboxDbContext в ApplicationDbContext
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<OutboxDbContext>(options => 
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.MigrationsAssembly("AuthService.Infrastructure")));

builder.Services.AddIdentity<AuthService.Domain.Entities.User, IdentityRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
    
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService.Application.Services.AuthService>();

builder.Services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
builder.Services.AddSingleton<JwtTokenGenerator>();

builder.Services.Configure<CookieSettings>(configuration.GetSection(CookieSettings.SectionName));
builder.Services.Configure<RabbitMQSettings>(configuration.GetSection(RabbitMQSettings.SectionName));
builder.Services.AddSingleton<RabbitMQConnectionFactory>();
builder.Services.AddScoped<IRabbitMQPublisher, RabbitMQPublisher>();

builder.Services.Configure<OutboxPublisherSettings>(configuration.GetSection(OutboxPublisherSettings.SectionName));
builder.Services.AddHostedService<OutboxPublisher>();

var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() 
                  ?? throw new InvalidOperationException("JwtSettings not configured");
                  
var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };
        
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var cookieSettings = context.HttpContext.RequestServices
                    .GetRequiredService<IOptions<CookieSettings>>().Value;
            
                var accessToken = context.Request.Cookies[cookieSettings.AccessTokenCookieName];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }
            
                return Task.CompletedTask;
            }
        };
    });
    
builder.Services.AddAuthorization();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDotNetRuntimeMetrics();

var corsOrigins = configuration.GetValue<string>("CORS_ORIGINS")?.Split(',') 
    ?? new[] { "http://localhost:8000", "http://localhost:3000", "http://127.0.0.1:8000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Ограничение частоты входа/регистрации/refresh: защита от перебора паролей и массовой регистрации.
// Сервис стоит за nginx, поэтому адрес клиента берём из заголовка прокси.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"error\":\"Too many attempts. Please try again in a minute.\"}", cancellationToken);
    };

    static string ClientKey(HttpContext httpContext)
    {
        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(realIp))
        {
            return realIp;
        }

        var forwarded = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    options.AddPolicy("auth-login", httpContext => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
        "login:" + ClientKey(httpContext),
        _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
        {
            PermitLimit = 15,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    options.AddPolicy("auth-register", httpContext => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
        "register:" + ClientKey(httpContext),
        _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0
        }));
});

var app = builder.Build();

using var scope = app.Services.CreateScope();
var applicationDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
var outboxDbContext = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

try
{
    applicationDbContext.Database.Migrate();
}
catch (Exception ex)
{
    logger.LogError(ex, "An error occurred while migrating the ApplicationDbContext.");
}

try
{
    outboxDbContext.Database.Migrate();
}
catch (Exception ex)
{
    logger.LogError(ex, "An error occurred while migrating the OutboxDbContext.");
}

app.UseRouting();
app.UsePrometheusHttpMetrics("auth-service");
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Описание API наружу в продакшене не отдаём
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapPrometheusScrapeEndpoint();

app.Run();