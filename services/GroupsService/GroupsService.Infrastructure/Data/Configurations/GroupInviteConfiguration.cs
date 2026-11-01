using GroupsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupsService.Infrastructure.Data.Configurations;

public class GroupInviteConfiguration : IEntityTypeConfiguration<GroupInvite>
{
    public void Configure(EntityTypeBuilder<GroupInvite> builder)
    {
        builder.ToTable("GroupInvites");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).IsRequired().HasMaxLength(50);
        builder.Property(i => i.GroupId).IsRequired().HasMaxLength(50);
        builder.Property(i => i.GroupName).IsRequired().HasMaxLength(100);
        builder.Property(i => i.ChannelId).HasMaxLength(50);
        builder.Property(i => i.ChannelName).HasMaxLength(100);
        builder.Property(i => i.FromUserId).IsRequired().HasMaxLength(50);
        builder.Property(i => i.FromNickName).IsRequired().HasMaxLength(50);
        builder.Property(i => i.ToNickName).IsRequired().HasMaxLength(50);
        builder.Property(i => i.BypassPassword).IsRequired();
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.HasIndex(i => i.ToNickName);
    }
}
