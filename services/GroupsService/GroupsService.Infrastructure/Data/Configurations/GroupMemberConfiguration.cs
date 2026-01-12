using GroupsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupsService.Infrastructure.Data.Configurations;

public class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
{
    public void Configure(EntityTypeBuilder<GroupMember> builder)
    {
        builder.ToTable("GroupMembers");
        
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
        
        builder.Property(m => m.Role)
            .IsRequired()
            .HasConversion<int>();
        
        builder.Property(m => m.CreatedAt)
            .IsRequired();
        
        builder.HasIndex(m => new { m.GroupId, m.UserId })
            .IsUnique();
        
        builder.HasIndex(m => m.UserId);
    }
}