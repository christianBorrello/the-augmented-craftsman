using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using TacBlog.Api;
using TacBlog.Api.Endpoints;
using TacBlog.Application.Features.Images;
using TacBlog.Application.Features.Posts;
using TacBlog.Application.Features.Comments;
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
        policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials()));

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
builder.Services.AddScoped<ICommentRepository, EfCommentRepository>();
builder.Services.AddScoped<PostComment>();
builder.Services.AddScoped<GetComments>();
builder.Services.AddScoped<GetCommentCount>();
builder.Services.AddScoped<DeleteComment>();
builder.Services.AddScoped<ListAdminComments>();
builder.Services.AddScoped<IReaderSessionRepository, EfReaderSessionRepository>();

var oauthSettings = new TacBlog.Infrastructure.Identity.OAuthSettings(
    GitHubClientId: builder.Configuration["OAuth:GitHub:ClientId"] ?? "",
    GitHubClientSecret: builder.Configuration["OAuth:GitHub:ClientSecret"] ?? "",
    GoogleClientId: builder.Configuration["OAuth:Google:ClientId"],
    GoogleClientSecret: builder.Configuration["OAuth:Google:ClientSecret"]
);

// Register validator
builder.Services.AddSingleton<TacBlog.Infrastructure.Identity.OAuthSettingsValidator>();

builder.Services.AddHttpClient();

if (builder.Environment.IsDevelopment())
    builder.Services.AddSingleton<IOAuthClient, DevOAuthClient>();
else
    builder.Services.AddSingleton<IOAuthClient>(sp =>
        new ProductionOAuthClient(oauthSettings, sp.GetRequiredService<IHttpClientFactory>().CreateClient()));

builder.Services.AddScoped<HandleOAuthCallback>();
builder.Services.AddScoped<CheckSession>();
builder.Services.AddScoped<InitiateOAuth>();
builder.Services.AddScoped<SignOut>();

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

    if (app.Configuration.GetValue("Database:RunMigrationsAtStartup", defaultValue: true))
        await db.Database.MigrateAsync();

    // Validate OAuth settings at startup
    var oauthValidator = scope.ServiceProvider.GetRequiredService<TacBlog.Infrastructure.Identity.OAuthSettingsValidator>();
    oauthValidator.Validate(oauthSettings, app.Environment.IsProduction());
}

app.UseSerilogRequestLogging();
app.UseCors();

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
app.MapTagEndpoints();
app.MapImageEndpoints();
app.MapLikeEndpoints();
app.MapCommentEndpoints();
app.MapOAuthEndpoints();

app.Run();

public partial class Program;
