using Microsoft.EntityFrameworkCore;
using TacBlog.Api.Endpoints;
using TacBlog.Application.Features.Auth;
using TacBlog.Application.Features.Posts;
using TacBlog.Application.Ports.Driven;
using TacBlog.Infrastructure;
using TacBlog.Infrastructure.Clock;
using TacBlog.Infrastructure.Identity;
using TacBlog.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddDbContext<TacBlogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IBlogPostRepository, EfBlogPostRepository>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<CreatePost>();
builder.Services.AddScoped<GetPostBySlug>();

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var section = config.GetSection("AdminCredentials");
    return new AdminCredentials(
        section["Email"] ?? throw new InvalidOperationException("AdminCredentials:Email is required"),
        section["HashedPassword"] ?? throw new InvalidOperationException("AdminCredentials:HashedPassword is required"));
});

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var section = config.GetSection("Jwt");
    return new JwtSettings(
        section["Secret"] ?? throw new InvalidOperationException("Jwt:Secret is required"),
        section["Issuer"] ?? "TacBlog",
        int.TryParse(section["ExpiryInMinutes"], out var expiry) ? expiry : 60);
});

builder.Services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
builder.Services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<LoginHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TacBlogDbContext>();
    await db.Database.MigrateAsync();
}

app.UseCors();
app.MapPostEndpoints();
app.MapAuthEndpoints();

app.Run();

public partial class Program;
