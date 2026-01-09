using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NcpAdminBlazor.Domain.AggregatesModel.RoleAggregate;
using NcpAdminBlazor.Domain.AggregatesModel.UserAggregate;

namespace NcpAdminBlazor.Infrastructure.EntityConfigurations;

internal sealed class UserEntityTypeConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .UseGuidVersion7ValueGenerator()
            .HasComment("用户标识");

        builder.Property(user => user.Username)
            .IsRequired()
            .HasMaxLength(50)
            .HasComment("用户名");

        builder.Property(user => user.Phone)
            .IsRequired()
            .HasMaxLength(20)
            .HasComment("手机号");

        builder.Property(user => user.PasswordHash)
            .IsRequired()
            .HasMaxLength(256)
            .HasComment("密码哈希");

        builder.Property(user => user.RealName)
            .IsRequired()
            .HasMaxLength(50)
            .HasComment("真实姓名");

        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("邮箱地址");

        builder.Property(user => user.AssignedRoleIds)
            .HasConversion(
                v => string.Join(',', v.Select(id => id.Id.ToString())),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => new RoleId(Guid.Parse(s)))
                    .ToList())
            .HasColumnType("text");

        builder.Property(user => user.RefreshToken)
            .IsRequired()
            .HasMaxLength(256)
            .HasComment("刷新令牌");

        builder.Property(user => user.RefreshExpiry)
            .IsRequired()
            .HasComment("刷新令牌到期时间");

        builder.Property(user => user.CreatedAt)
            .IsRequired()
            .HasComment("创建时间");

        builder.Property(user => user.IsDeleted)
            .IsRequired()
            .HasComment("是否删除");

        builder.Property(user => user.DeletedAt)
            .IsRequired()
            .HasComment("删除时间");

        builder.HasIndex(user => user.Username)
            .IsUnique();
    }
}