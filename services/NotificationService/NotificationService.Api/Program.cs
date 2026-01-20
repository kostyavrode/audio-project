using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NotificationService.Api.Hubs;
using NotificationService.Api.Services;
using NotificationService.Application.Services;
using NotificationService.Infrastructure.Messaging;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using NotificationServiceClass = NotificationService.Api.Services.NotificationService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwtSecret = builder.Configuration.GetValue<string>("JWT_SECRET") 
    ?? throw new InvalidOperationException("JWT_SECRET is not configured");

var jwtIssuer = builder.Configuration.GetValue<string>("JWT_ISSUER") ?? "AuthService";
var jwtAudience = builder.Configuration.GetValue<string>("JWT_AUDIENCE") ?? "GroupChatApp";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                string? token = null;
                
                // Пробуем получить токен из Authorization header
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader))
                {
                    if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        token = authHeader.Substring("Bearer ".Length).Trim();
                    }
                    else
                    {
                        var parts = authHeader.Split(" ");
                        if (parts.Length > 1)
                        {
                            token = parts.Last();
                        }
                        else if (parts.Length == 1)
                        {
                            token = parts[0];
                        }
                    }
                }
                
                // Пробуем получить токен из cookies
                if (string.IsNullOrEmpty(token))
                {
                    token = context.Request.Cookies["access_token"];
                }
                
                // Пробуем получить токен из query string (для SignalR)
                if (string.IsNullOrEmpty(token) && context.Request.Path.StartsWithSegments("/hubs/notification"))
                {
                    token = context.Request.Query["access_token"];
                }
                
                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }
                
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSignalR();

builder.Services.AddScoped<INotificationService, NotificationServiceClass>();

builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection(RabbitMQSettings.SectionName));
builder.Services.AddSingleton<RabbitMQConnectionFactory>();
builder.Services.AddHostedService<RabbitMQConsumer>();

var corsOrigins = builder.Configuration.GetValue<string>("CORS_ORIGINS")?.Split(',') 
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

var app = builder.Build();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notification");

app.Run();
