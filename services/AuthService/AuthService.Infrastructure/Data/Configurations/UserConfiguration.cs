using AuthService.Domain.Entities;
using AuthService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
        public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        
        // Настройка Id (наследуется от IdentityUser, но можно переопределить)
        builder.HasKey(u => u.Id);
        
        // Email - Value Object
        // Преобразуем Email Value Object в string для хранения в БД
        builder.Property(u => u.Email)
            .HasConversion(
                v => v.Value,
                v => Email.Create(v)
            )
            .HasMaxLength(254)
            .IsRequired();
        
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");
        
        // NickName - Value Object
        builder.Property(u => u.NickName)
            .HasConversion(
                nickName => nickName.Value,
                value => NickName.Create(value)
            )
            .HasMaxLength(30)
            .IsRequired();
        
        // Уникальный индекс на NickName (case-insensitive через БД, если возможно)
        builder.HasIndex(u => u.NickName)
            .IsUnique()
            .HasDatabaseName("IX_Users_NickName");
        // Примечание: PostgreSQL поддерживает case-insensitive индексы через lower()
        
        // PasswordHash
        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(256); // ASP.NET Core Identity использует BCrypt, который дает ~60 символов, но оставляем запас
        
        // RefreshToken - Value Object (nullable)
        // Сохраняем как JSON или отдельные поля
        // Вариант 1: JSON (проще, но менее эффективно для запросов)
        builder.OwnsOne(u => u.RefreshToken, rt =>
        {
            rt.Property(r => r.Token)
                .HasColumnName("RefreshToken")
                .HasMaxLength(512);
            
            rt.Property(r => r.ExpiresAt)
                .HasColumnName("RefreshTokenExpiresAt");
            
            rt.Property(r => r.CreatedAt)
                .HasColumnName("RefreshTokenCreatedAt");
            
            rt.Property(r => r.IsRevoked)
                .HasColumnName("RefreshTokenIsRevoked")
                .HasDefaultValue(false);
        });
        
        // Игнорируем доменные события - они не хранятся в БД напрямую
        // (будут обрабатываться через Outbox Pattern)
        builder.Ignore(u => u.DomainEvents);
        
        // CreatedAt и UpdatedAt
        builder.Property(u => u.CreatedAt)
            .IsRequired();
        
        builder.Property(u => u.UpdatedAt);
    }
}