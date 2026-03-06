using Microsoft.EntityFrameworkCore;
using TacBlog.Api.Endpoints;
using TacBlog.Application.Features.Posts;
using TacBlog.Application.Ports.Driven;
using TacBlog.Infrastructure;
using TacBlog.Infrastructure.Clock;
using TacBlog.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TacBlogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IBlogPostRepository, EfBlogPostRepository>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<CreatePost>();
builder.Services.AddScoped<GetPostBySlug>();

var app = builder.Build();

app.MapPostEndpoints();

app.Run();

public partial class Program;
