using GroupsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupsService.Infrastructure.Data.Configurations;

public class GroupNoticeConfiguration : IEntityTypeConfiguration<GroupNotice>
{
    public void Configure(EntityTypeBuilder<GroupNotice> builder)
    {
        builder.ToTable("GroupNotices");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).IsRequired().HasMaxLength(50);
        builder.Property(n => n.GroupId).IsRequired().HasMaxLength(50);
        builder.Property(n => n.GroupName).IsRequired().HasMaxLength(100);
        builder.Property(n => n.FromNickName).IsRequired().HasMaxLength(50);
        builder.Property(n => n.ToUserId).IsRequired().HasMaxLength(50);
        builder.Property(n => n.Text).IsRequired().HasMaxLength(GroupNotice.MaxTextLength);
        builder.Property(n => n.CreatedAt).IsRequired();
        builder.HasIndex(n => n.ToUserId);
    }
}
