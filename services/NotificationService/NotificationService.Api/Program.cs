using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NotificationService.Api.Hubs;
using NotificationService.Application.Services;
using System.Text;
using Microsoft.AspNetCore.SignalR;

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
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/notification"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSignalR();

builder.Services.AddScoped<INotificationService, Api.Services.NotificationService>();

builder.Services.Configure<Infrastructure.Messaging.RabbitMQSettings>(builder.Configuration.GetSection(Infrastructure.Messaging.RabbitMQSettings.SectionName));
builder.Services.AddSingleton<Infrastructure.Messaging.RabbitMQConnectionFactory>();
builder.Services.AddHostedService<Infrastructure.Messaging.RabbitMQConsumer>();

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
