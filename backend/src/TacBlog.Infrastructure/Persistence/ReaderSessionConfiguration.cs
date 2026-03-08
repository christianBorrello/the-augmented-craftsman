using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TacBlog.Domain;

namespace TacBlog.Infrastructure.Persistence;

public sealed class ReaderSessionConfiguration : IEntityTypeConfiguration<ReaderSession>
{
    public void Configure(EntityTypeBuilder<ReaderSession> builder)
    {
        builder.ToTable("reader_sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(s => s.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.AvatarUrl)
            .HasColumnName("avatar_url")
            .HasMaxLength(2048);

        builder.Property(s => s.Provider)
            .HasColumnName("provider")
            .HasMaxLength(10)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(s => s.ProviderId)
            .HasColumnName("provider_id")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(s => s.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .IsRequired();

        builder.HasIndex(s => s.ExpiresAtUtc)
            .HasDatabaseName("ix_reader_sessions_expires");
    }
}
