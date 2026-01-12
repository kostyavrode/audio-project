using Microsoft.EntityFrameworkCore;

namespace AudioService.Infrastructure.Outbox;

public class OutboxDbContext : DbContext
{
    public OutboxDbContext(DbContextOptions<OutboxDbContext> options) : base(options)
    {
    }

    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.Status, e.CreatedAt })
                .HasDatabaseName("IX_OutboxMessages_Status_CreatedAt");

            entity.HasIndex(e => e.EventId)
                .IsUnique()
                .HasDatabaseName("IX_OutboxMessages_EventId");

            entity.Property(e => e.EventType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Payload)
                .HasColumnType("jsonb")
                .IsRequired();

            entity.Property(e => e.Status)
                .HasConversion<int>();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(1000);
        });
    }
}
