using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// JWT Authentication
var jwtSecret = builder.Configuration.GetValue<string>("JWT_SECRET") 
    ?? builder.Configuration["JwtSettings:SecretKey"]
    ?? throw new InvalidOperationException("JWT_SECRET or JwtSettings:SecretKey not configured");

var jwtIssuer = builder.Configuration.GetValue<string>("JWT_ISSUER") 
    ?? builder.Configuration["JwtSettings:Issuer"]
    ?? "AuthService";

var jwtAudience = builder.Configuration.GetValue<string>("JWT_AUDIENCE") 
    ?? builder.Configuration["JwtSettings:Audience"]
    ?? "GroupChatApp";

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
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
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

// SignalR
builder.Services.AddSignalR();

// RabbitMQ Configuration
builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));

// CORS
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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notification");

app.Run();

// NotificationHub implementation
[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            _logger?.LogInformation("User {UserId} connected to NotificationHub", userId);
        }
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            _logger?.LogInformation("User {UserId} disconnected from NotificationHub", userId);
        }
        return base.OnDisconnectedAsync(exception);
    }

    public async Task JoinGroup(string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId))
        {
            await Clients.Caller.SendAsync("Error", "Group ID is required");
            return;
        }

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        _logger?.LogInformation("User {UserId} joined group {GroupId} in NotificationHub", userId, groupId);
        
        await Clients.Caller.SendAsync("JoinedGroup", groupId);
    }

    public async Task LeaveGroup(string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId))
        {
            await Clients.Caller.SendAsync("Error", "Group ID is required");
            return;
        }

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
        _logger?.LogInformation("User {UserId} left group {GroupId} in NotificationHub", userId, groupId);
        
        await Clients.Caller.SendAsync("LeftGroup", groupId);
    }

    private string? GetUserId()
    {
        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value
            ?? Context.User?.FindFirst("userId")?.Value;
    }
}

// Placeholder for RabbitMQSettings - needs to be implemented
public class RabbitMQSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
}
