using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TacBlog.Domain;

namespace TacBlog.Infrastructure.Persistence;

public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("tags");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                guid => new TagId(guid));

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .HasMaxLength(50)
            .IsRequired()
            .HasConversion(
                name => name.Value,
                value => new TagName(value));

        builder.Property(t => t.Slug)
            .HasColumnName("slug")
            .HasMaxLength(100)
            .IsRequired()
            .HasConversion(
                slug => slug.Value,
                value => new Slug(value));

        builder.HasIndex(t => t.Slug)
            .IsUnique();
    }
}
