using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll.Microsoft.Extensions.DependencyInjection;
using TacBlog.Acceptance.Tests.Contexts;
using TacBlog.Acceptance.Tests.Drivers;

namespace TacBlog.Acceptance.Tests.Support;

public static class DependencyConfig
{
    private static readonly TacBlogWebApplicationFactory Factory = new();

    [ScenarioDependencies]
    public static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton(Factory);
        services.AddScoped(_ => Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        }));

        services.AddScoped<ApiContext>();
        services.AddScoped<AuthContext>();
        services.AddScoped<PostApiDriver>();
        services.AddScoped<TagApiDriver>();
        services.AddScoped<AuthApiDriver>();
        services.AddScoped<ImageApiDriver>();

        return services;
    }
}
