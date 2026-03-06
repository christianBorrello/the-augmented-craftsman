using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Reqnroll.Microsoft.Extensions.DependencyInjection;
using TacBlog.Acceptance.Tests.Contexts;
using TacBlog.Acceptance.Tests.Drivers;

namespace TacBlog.Acceptance.Tests.Support;

public static class DependencyConfig
{
    internal static TacBlogWebApplicationFactory Factory { get; set; } = null!;

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

[Binding]
public sealed class TestRunHooks
{
    [BeforeTestRun]
    public static async Task StartInfrastructure()
    {
        var factory = new TacBlogWebApplicationFactory();
        await factory.StartContainerAsync();
        DependencyConfig.Factory = factory;
    }

    [AfterTestRun]
    public static async Task StopInfrastructure()
    {
        if (DependencyConfig.Factory is not null)
            await DependencyConfig.Factory.DisposeAsync();
    }
}
