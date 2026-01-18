using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.DbContext;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options)
    { }
    
    public DbSet<UserConnection> UserConnections { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        
        modelBuilder.Entity<UserConnection>(entity =>
        {
            entity.ToTable("UserConnections");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ConnectionId).IsUnique();
            
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.ConnectionId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.ConnectedAt).IsRequired();
            entity.Property(e => e.LastActivityAt).IsRequired();
        });
    }
}