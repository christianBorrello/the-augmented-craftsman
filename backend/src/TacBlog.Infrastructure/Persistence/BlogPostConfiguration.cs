using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TacBlog.Domain;

namespace TacBlog.Infrastructure.Persistence;

public sealed class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.ToTable("blog_posts");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                guid => new PostId(guid));

        builder.Property(p => p.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired()
            .HasConversion(
                title => title.Value,
                value => new Title(value));

        builder.Property(p => p.Slug)
            .HasColumnName("slug")
            .HasMaxLength(250)
            .IsRequired()
            .HasConversion(
                slug => slug.Value,
                value => new Slug(value));

        builder.HasIndex(p => p.Slug)
            .IsUnique();

        builder.Property(p => p.Content)
            .HasColumnName("content")
            .HasColumnType("text")
            .IsRequired()
            .HasConversion(
                content => content.Value,
                value => new PostContent(value));

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(
                status => status.ToString(),
                value => Enum.Parse<PostStatus>(value));

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
