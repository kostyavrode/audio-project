using ChatService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatService.Infrastructure.Data.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");
        
        builder.HasKey(m => m.Id);
        
        builder.Property(m => m.Id)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(m => m.GroupId)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(m => m.UserId)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(m => m.Content)
            .IsRequired()
            .HasMaxLength(2000);
        
        builder.Property(m => m.CreatedAt)
            .IsRequired();
        
        builder.HasIndex(m => m.GroupId);
        builder.HasIndex(m => new { m.GroupId, m.CreatedAt });
        builder.HasIndex(m => m.UserId);
    }
}