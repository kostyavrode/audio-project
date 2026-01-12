using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GroupsService.Infrastructure.Outbox;

public class OutboxDbContextFactory : IDesignTimeDbContextFactory<OutboxDbContext>
{
    public OutboxDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OutboxDbContext>();
        
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=GroupsServiceDb_Dev;Username=postgres;Password=postgres;";
        
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsqlOptions => npgsqlOptions
                .MigrationsAssembly("GroupsService.Infrastructure")
                .MigrationsHistoryTable("__EFMigrationsHistory", "public")
        );
        
        return new OutboxDbContext(optionsBuilder.Options);
    }
}
