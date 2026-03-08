using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using TacBlog.Api.Endpoints;
using TacBlog.Application.Features.Auth;
using TacBlog.Application.Features.Images;
using TacBlog.Application.Features.Posts;
using TacBlog.Application.Features.Likes;
using TacBlog.Application.Features.OAuth;
using TacBlog.Application.Features.Tags;
using TacBlog.Application.Ports.Driven;
using TacBlog.Infrastructure;
using TacBlog.Infrastructure.Clock;
using TacBlog.Infrastructure.Identity;
using TacBlog.Infrastructure.Persistence;
using TacBlog.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

var allowedOrigins = builder.Environment.IsDevelopment()
    ? new[] { "http://localhost:4321" }
    : new[] { "https://theaugmentedcraftsman.christianborrello.dev" };

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddDbContext<TacBlogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IBlogPostRepository, EfBlogPostRepository>();
builder.Services.AddScoped<ITagRepository, EfTagRepository>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<CreatePost>();
builder.Services.AddScoped<GetPostBySlug>();
builder.Services.AddScoped<EditPost>();
builder.Services.AddScoped<DeletePost>();
builder.Services.AddScoped<PublishPost>();
builder.Services.AddScoped<ListPosts>();
builder.Services.AddScoped<PreviewPost>();
builder.Services.AddScoped<BrowsePublishedPosts>();
builder.Services.AddScoped<ReadPublishedPost>();
builder.Services.AddScoped<FilterPostsByTag>();
builder.Services.AddScoped<GetRelatedPosts>();
builder.Services.AddScoped<CreateTag>();
builder.Services.AddScoped<ListTags>();
builder.Services.AddScoped<RenameTag>();
builder.Services.AddScoped<DeleteTag>();
builder.Services.AddScoped<BrowsePublicTags>();
builder.Services.AddScoped<ILikeRepository, EfLikeRepository>();
builder.Services.AddScoped<LikePost>();
builder.Services.AddScoped<UnlikePost>();
builder.Services.AddScoped<GetLikeCount>();
builder.Services.AddScoped<CheckIfLiked>();
builder.Services.AddScoped<IReaderSessionRepository, EfReaderSessionRepository>();
builder.Services.AddSingleton<IOAuthClient>(sp =>
    throw new InvalidOperationException("Configure OAuth providers for production"));
builder.Services.AddScoped<HandleOAuthCallback>();
builder.Services.AddScoped<CheckSession>();

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var section = config.GetSection("ImageKit");
    return new ImageKitSettings(
        section["UrlEndpoint"] ?? "https://ik.imagekit.io/default",
        section["PublicKey"] ?? "public_key_not_configured",
        section["PrivateKey"] ?? "private_key_not_configured");
});
builder.Services.AddSingleton<IImageStorage, ImageKitImageStorage>();
builder.Services.AddScoped<UploadImage>();
builder.Services.AddScoped<SetFeaturedImage>();
builder.Services.AddScoped<RemoveFeaturedImage>();

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var section = config.GetSection("AdminCredentials");
    var email = section["Email"];
    var hashedPassword = section["HashedPassword"];

    if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(hashedPassword))
        return new AdminCredentials(email, hashedPassword);

    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException(
            "AdminCredentials:Email and AdminCredentials:HashedPassword are required in production.");

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

    if (string.IsNullOrEmpty(secret) && !builder.Environment.IsDevelopment())
        throw new InvalidOperationException(
            "Jwt:Secret is required in production. Set it via environment variable Jwt__Secret.");

    secret ??= "dev-only-jwt-secret-that-must-not-be-used-in-production-minimum-length";

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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "An unexpected error occurred.",
                status = 500
            });
        });
    });
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TacBlogDbContext>();
    await db.Database.MigrateAsync();
}

app.UseSerilogRequestLogging();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapGet("/health/ready", async (TacBlogDbContext db) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new { status = "ready", database = "connected" });
    }
    catch
    {
        return Results.Json(
            new { status = "unhealthy", database = "disconnected" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapPostEndpoints();
app.MapAuthEndpoints();
app.MapTagEndpoints();
app.MapImageEndpoints();
app.MapLikeEndpoints();
app.MapOAuthEndpoints();

app.Run();

public partial class Program;
