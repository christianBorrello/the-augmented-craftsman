using Microsoft.EntityFrameworkCore;
using TacBlog.Domain;
using TacBlog.Infrastructure.Persistence;

namespace TacBlog.Infrastructure;

public sealed class TacBlogDbContext(DbContextOptions<TacBlogDbContext> options) : DbContext(options)
{
    public DbSet<BlogPost> Posts => Set<BlogPost>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<ReaderSession> ReaderSessions => Set<ReaderSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BlogPostConfiguration());
        modelBuilder.ApplyConfiguration(new TagConfiguration());
        modelBuilder.ApplyConfiguration(new LikeConfiguration());
        modelBuilder.ApplyConfiguration(new CommentConfiguration());
        modelBuilder.ApplyConfiguration(new ReaderSessionConfiguration());
    }
}
