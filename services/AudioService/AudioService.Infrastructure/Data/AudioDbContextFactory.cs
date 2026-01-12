using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AudioService.Infrastructure.Data;

public class AudioDbContextFactory : IDesignTimeDbContextFactory<AudioDbContext>
{
    public AudioDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AudioDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=AudioServiceDb_Dev;Username=postgres;Password=postgres;";

        optionsBuilder.UseNpgsql(
            connectionString,
            npgsqlOptions => npgsqlOptions
                .MigrationsAssembly("AudioService.Infrastructure")
                .MigrationsHistoryTable("__EFMigrationsHistory", "public")
        );

        return new AudioDbContext(optionsBuilder.Options);
    }
}
