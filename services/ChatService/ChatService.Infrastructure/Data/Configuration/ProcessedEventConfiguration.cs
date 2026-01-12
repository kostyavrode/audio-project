using ChatService.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatService.Infrastructure.Data.Configurations;

public class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.ToTable("ProcessedEvents");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.EventId)
            .IsRequired();
        
        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(e => e.ProcessedAt)
            .IsRequired();
        
        builder.HasIndex(e => e.EventId)
            .IsUnique()
            .HasDatabaseName("IX_ProcessedEvents_EventId");
        
        builder.HasIndex(e => e.ProcessedAt);
    }
}