using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TacBlog.Domain;

namespace TacBlog.Infrastructure.Persistence;

public sealed class PageViewConfiguration : IEntityTypeConfiguration<PageView>
{
    public void Configure(EntityTypeBuilder<PageView> builder)
    {
        builder.ToTable("page_views");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .HasColumnName("id");

        builder.Property(v => v.PostSlug)
            .HasColumnName("post_slug")
            .HasMaxLength(250)
            .IsRequired()
            .HasConversion(
                slug => slug.Value,
                value => new Slug(value));

        builder.Property(v => v.VisitorId)
            .HasColumnName("visitor_id")
            .IsRequired()
            .HasConversion(
                id => id.Value,
                guid => new VisitorId(guid));

        builder.Property(v => v.ViewedAtUtc)
            .HasColumnName("viewed_at_utc")
            .IsRequired();

        builder.HasOne<BlogPost>()
            .WithMany()
            .HasForeignKey(v => v.PostSlug)
            .HasPrincipalKey(p => p.Slug)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
