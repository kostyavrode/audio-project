using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuthService.Infrastructure.Outbox;

/// <summary>
/// Factory для создания OutboxDbContext во время разработки (для миграций).
/// Используется EF Core Tools при создании миграций.
/// </summary>
public class OutboxDbContextFactory : IDesignTimeDbContextFactory<OutboxDbContext>
{
    public OutboxDbContext CreateDbContext(string[] args)
    {
        // Создаем опции для DbContext
        var optionsBuilder = new DbContextOptionsBuilder<OutboxDbContext>();
        
        // Используем строку подключения из переменных окружения или дефолтную
        // В production это будет из appsettings.json через DI
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=AuthServiceDb;Username=postgres;Password=postgres;";
        
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsqlOptions => npgsqlOptions.MigrationsAssembly("AuthService.Infrastructure")
        );
        
        return new OutboxDbContext(optionsBuilder.Options);
    }
}
