using AudioService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AudioService.Infrastructure.Data.Configuration;

public class AudioChannelConfiguration : IEntityTypeConfiguration<AudioChannel>
{
    public void Configure(EntityTypeBuilder<AudioChannel> builder)
    {
        builder.ToTable("AudioChannels");
        
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.Id).IsRequired().HasMaxLength(50);
        
        builder.Property(c => c.GroupId).IsRequired().HasMaxLength(50);
        
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        
        builder.Property(c => c.JanusRoomId);
        
        builder.Property(c => c.CreatedAt).IsRequired();
        
        builder.Property(c => c.UpdatedAt);
        
        builder.HasIndex(c => c.GroupId);
    }
}
