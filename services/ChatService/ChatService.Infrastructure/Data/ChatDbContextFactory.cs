using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ChatService.Infrastructure.Data;

public class ChatDbContextFactory : IDesignTimeDbContextFactory<ChatDbContext>
{
    public ChatDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ChatDbContext>();
        
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? "Host=localhost;Port=5432;Database=ChatServiceDb_Dev;Username=postgres;Password=postgres;";
        
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsqlOptions => npgsqlOptions
                .MigrationsAssembly("ChatService.Infrastructure")
                .MigrationsHistoryTable("__EFMigrationsHistory", "public")
        );
        
        return new ChatDbContext(optionsBuilder.Options);
    }
}