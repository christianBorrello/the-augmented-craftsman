using Microsoft.EntityFrameworkCore;

namespace TacBlog.Infrastructure;

public sealed class TacBlogDbContext(DbContextOptions<TacBlogDbContext> options) : DbContext(options);
