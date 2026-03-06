using Microsoft.EntityFrameworkCore;
using TacBlog.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TacBlogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.Run();

public partial class Program;
