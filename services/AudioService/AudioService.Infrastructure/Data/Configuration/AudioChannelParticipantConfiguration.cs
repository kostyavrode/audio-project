using AudioService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AudioService.Infrastructure.Data.Configuration;

public class AudioChannelParticipantConfiguration : IEntityTypeConfiguration<AudioChannelParticipant>
{
    public void Configure(EntityTypeBuilder<AudioChannelParticipant> builder)
    {
        builder.ToTable("AudioChannelParticipants");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Id).IsRequired().HasMaxLength(50);
        
        builder.Property(p => p.ChannelId).IsRequired().HasMaxLength(50);
        
        builder.Property(p => p.UserId).IsRequired().HasMaxLength(50);
        
        builder.Property(p => p.JoinedAt).IsRequired();
        
        builder.Property(p => p.LeftAt);
        
        builder.Property(p => p.CreatedAt).IsRequired();
        
        builder.Property(p => p.UpdatedAt);
        
        builder.HasIndex(p => new { p.ChannelId, p.UserId })
            .HasDatabaseName("IX_AudioChannelParticipants_ChannelId_UserId");
        
        builder.HasIndex(p => p.ChannelId);
        builder.HasIndex(p => p.UserId);
    }
}
