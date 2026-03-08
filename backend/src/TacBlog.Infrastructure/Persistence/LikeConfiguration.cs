using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TacBlog.Domain;

namespace TacBlog.Infrastructure.Persistence;

public sealed class LikeConfiguration : IEntityTypeConfiguration<Like>
{
    public void Configure(EntityTypeBuilder<Like> builder)
    {
        builder.ToTable("likes");

        builder.HasKey(l => new { l.PostSlug, l.VisitorId });

        builder.Property(l => l.PostSlug)
            .HasColumnName("post_slug")
            .HasMaxLength(250)
            .IsRequired()
            .HasConversion(
                slug => slug.Value,
                value => new Slug(value));

        builder.Property(l => l.VisitorId)
            .HasColumnName("visitor_id")
            .IsRequired()
            .HasConversion(
                id => id.Value,
                guid => new VisitorId(guid));

        builder.Property(l => l.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasOne<BlogPost>()
            .WithMany()
            .HasForeignKey(l => l.PostSlug)
            .HasPrincipalKey(p => p.Slug)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
