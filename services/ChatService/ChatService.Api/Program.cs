using ChatService.Infrastructure.Data;
using ChatService.Infrastructure.Messaging;
using ChatService.Infrastructure.Outbox;
using ChatService.Domain.Interfaces;
using ChatService.Infrastructure.Repository;
using ChatService.Application.Services;
using ChatService.Application.Validators;
using ChatService.Api.Hubs;
using ChatService.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<OutboxDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];
if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("SecretKey not configured");
}

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
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
    
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            string? token = null;
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
            
            if (string.IsNullOrEmpty(token))
            {
                token = context.Request.Cookies["access_token"];
            }
            
            if (string.IsNullOrEmpty(token) && context.Request.Path.StartsWithSegments("/hubs"))
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

builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection(RabbitMQSettings.SectionName));
builder.Services.AddSingleton<RabbitMQConnectionFactory>();
builder.Services.AddScoped<IRabbitMQPublisher, RabbitMQPublisher>();

builder.Services.Configure<OutboxPublisherSettings>(builder.Configuration.GetSection(OutboxPublisherSettings.SectionName));
builder.Services.AddHostedService<OutboxPublisher>();

builder.Services.AddHostedService<RabbitMQConsumer>();

builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IGroupMemberRepository, GroupMemberRepository>();

builder.Services.AddScoped<IMessageService, MessageService>();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<SendMessageDtoValidator>();

builder.Services.AddSignalR();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
var outboxDbContext = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
try
{
    dbContext.Database.Migrate();
    outboxDbContext.Database.Migrate();
}
catch (Exception ex)
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while migrating the database.");
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
