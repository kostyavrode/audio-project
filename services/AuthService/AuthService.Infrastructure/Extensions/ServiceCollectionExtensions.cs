using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Data;
using AuthService.Infrastructure.Messaging;
using AuthService.Infrastructure.Outbox;
using AuthService.Infrastructure.Repositories;
using AuthService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.MigrationsAssembly("AuthService.Infrastructure")
            ));
        
        services.AddDbContext<OutboxDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.MigrationsAssembly("AuthService.Infrastructure")
            ));
        
        services.AddScoped<IUserRepository, UserRepository>();
        
        services.Configure<JwtSettings>(
            configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<JwtTokenGenerator>();
        
        services.Configure<RabbitMQSettings>(
            configuration.GetSection(RabbitMQSettings.SectionName));
        services.AddSingleton<RabbitMQConnectionFactory>();
        services.AddScoped<IRabbitMQPublisher, RabbitMQPublisher>();
        
        services.Configure<OutboxPublisherSettings>(
            configuration.GetSection(OutboxPublisherSettings.SectionName));
        services.AddHostedService<OutboxPublisher>();
        
        return services;
    }
}