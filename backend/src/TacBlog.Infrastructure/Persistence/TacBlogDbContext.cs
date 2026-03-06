using Microsoft.EntityFrameworkCore;
using TacBlog.Domain;
using TacBlog.Infrastructure.Persistence;

namespace TacBlog.Infrastructure;

public sealed class TacBlogDbContext(DbContextOptions<TacBlogDbContext> options) : DbContext(options)
{
    public DbSet<BlogPost> Posts => Set<BlogPost>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BlogPostConfiguration());
    }
}
