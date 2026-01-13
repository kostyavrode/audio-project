using AudioService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AudioService.Infrastructure.Data.Configuration;

public class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
{
    public void Configure(EntityTypeBuilder<GroupMember> builder)
    {
        builder.ToTable("GroupMembers");
        
        builder.HasKey(m => m.Id);
        
        builder.Property(m => m.Id).IsRequired().HasMaxLength(50);
        
        builder.Property(m => m.GroupId).IsRequired().HasMaxLength(50);
        
        builder.Property(m => m.UserId).IsRequired().HasMaxLength(50);
        
        builder.Property(m => m.Role).HasConversion<int>().IsRequired();
        
        builder.Property(m => m.CreatedAt).IsRequired();
        
        builder.HasIndex(m => new { m.GroupId, m.UserId })
            .IsUnique()
            .HasDatabaseName("IX_GroupMembers_GroupId_UserId");
        
        builder.HasIndex(m => m.GroupId);
        builder.HasIndex(m => m.UserId);
    }
}
