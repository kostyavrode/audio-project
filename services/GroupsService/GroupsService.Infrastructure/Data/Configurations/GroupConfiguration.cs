using GroupsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupsService.Infrastructure.Data.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("Groups");
        
        builder.HasKey(g => g.Id);
        
        builder.Property(g => g.Id).IsRequired().HasMaxLength(50);
        
        builder.Property(g => g.Name).IsRequired().HasMaxLength(100);
        
        builder.Property(g => g.Description).HasMaxLength(500);
        
        builder.Property(g => g.PasswordHash).HasMaxLength(255);
        
        builder.Property(g => g.OwnerId).IsRequired().HasMaxLength(50);
        
        builder.Property(g => g.CreatedAt).IsRequired();    
        
        builder.Property(g => g.UpdatedAt);
        
        builder.HasMany(g => g.Members).WithOne(m => m.Group).HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(g => new { g.OwnerId});
    }
}