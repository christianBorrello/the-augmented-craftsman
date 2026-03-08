using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TacBlog.Domain;

namespace TacBlog.Infrastructure.Persistence;

public sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("comments");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .IsRequired()
            .HasConversion(
                id => id.Value,
                guid => new CommentId(guid));

        builder.Property(c => c.PostSlug)
            .HasColumnName("post_slug")
            .HasMaxLength(250)
            .IsRequired()
            .HasConversion(
                slug => slug.Value,
                value => new Slug(value));

        builder.Property(c => c.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.AvatarUrl)
            .HasColumnName("avatar_url")
            .HasMaxLength(2048);

        builder.Property(c => c.Provider)
            .HasColumnName("provider")
            .HasMaxLength(10)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(c => c.Text)
            .HasColumnName("text")
            .HasMaxLength(2000)
            .IsRequired()
            .HasConversion(
                text => text.Value,
                value => new CommentText(value));

        builder.Property(c => c.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasOne<BlogPost>()
            .WithMany()
            .HasForeignKey(c => c.PostSlug)
            .HasPrincipalKey(p => p.Slug)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.PostSlug, c.CreatedAtUtc })
            .HasDatabaseName("ix_comments_post_slug_created");
    }
}
