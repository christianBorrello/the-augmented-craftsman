using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
builder.Services.AddScoped<EditPost>();
builder.Services.AddScoped<DeletePost>();
builder.Services.AddScoped<PublishPost>();
builder.Services.AddScoped<ListPosts>();
builder.Services.AddScoped<PreviewPost>();

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var section = config.GetSection("AdminCredentials");
    var email = section["Email"];
    var hashedPassword = section["HashedPassword"];

    if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(hashedPassword))
        return new AdminCredentials(email, hashedPassword);

    var passwordHasher = new AspNetPasswordHasher();
    return new AdminCredentials(
        "admin@localhost",
        passwordHasher.Hash("dev-admin-password"));
});

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var section = config.GetSection("Jwt");
    var secret = section["Secret"];

    if (string.IsNullOrEmpty(secret))
        secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    return new JwtSettings(
        secret,
        section["Issuer"] ?? "TacBlog",
        int.TryParse(section["ExpiryInMinutes"], out var expiry) ? expiry : 60);
});

builder.Services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
builder.Services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();
builder.Services.AddSingleton<LoginHandler>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<JwtSettings>((options, settings) =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = settings.Issuer,
            ValidAudience = settings.Issuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TacBlogDbContext>();
    await db.Database.MigrateAsync();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapPostEndpoints();
app.MapAuthEndpoints();
app.MapTagEndpoints();

app.Run();

public partial class Program;
