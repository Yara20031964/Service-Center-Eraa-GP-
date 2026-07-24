using KHDMA.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace KHDMA.Tests;

/// <summary>
/// Covers the resolver itself. Every other test injects a passthrough stub, so
/// without these the config/request/idempotency behaviour would ship unexercised.
/// </summary>
public class ImageUrlResolverTests
{
    private static ImageUrlResolver Build(string? publicBaseUrl = null, HttpContext? context = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(publicBaseUrl is null
                ? []
                : new Dictionary<string, string?> { ["App:PublicBaseUrl"] = publicBaseUrl })
            .Build();

        var accessor = new HttpContextAccessor { HttpContext = context };
        return new ImageUrlResolver(config, accessor);
    }

    private static HttpContext RequestOn(string scheme, string host, string pathBase = "")
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = scheme;
        ctx.Request.Host = new HostString(host);
        ctx.Request.PathBase = pathBase;
        return ctx;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankPathsArePassedThrough(string? path)
    {
        Assert.Equal(path, Build("https://cdn.example.com").Resolve(path));
    }

    [Theory]
    [InlineData("http://other.host/uploads/a.jpg")]
    [InlineData("https://other.host/uploads/a.jpg")]
    [InlineData("HTTPS://Other.Host/uploads/a.jpg")]
    public void AlreadyAbsoluteUrlsAreLeftAlone(string absolute)
    {
        // Idempotence matters: a value can pass through more than one mapping
        // layer, and prefixing twice would produce a broken URL.
        Assert.Equal(absolute, Build("https://cdn.example.com").Resolve(absolute));
    }

    [Fact]
    public void ConfiguredBaseUrlWinsOverTheRequest()
    {
        var resolver = Build("https://cdn.example.com", RequestOn("http", "localhost:5283"));

        Assert.Equal("https://cdn.example.com/uploads/p/a.jpg", resolver.Resolve("/uploads/p/a.jpg"));
    }

    [Fact]
    public void RequestOriginIsUsedWhenNoBaseUrlIsConfigured()
    {
        var resolver = Build(context: RequestOn("https", "khdma.runasp.net"));

        Assert.Equal("https://khdma.runasp.net/uploads/p/a.jpg", resolver.Resolve("/uploads/p/a.jpg"));
    }

    [Fact]
    public void PathBaseIsPreserved()
    {
        var resolver = Build(context: RequestOn("https", "host.example", "/api-root"));

        Assert.Equal("https://host.example/api-root/uploads/a.jpg", resolver.Resolve("/uploads/a.jpg"));
    }

    [Fact]
    public void RelativePathSurvivesWhenThereIsNoBaseUrlAndNoRequest()
    {
        // The background workers resolve outside any request. Returning the stored
        // value beats throwing, and the row stays usable once a base URL exists.
        Assert.Equal("/uploads/a.jpg", Build().Resolve("/uploads/a.jpg"));
    }

    [Theory]
    [InlineData("https://cdn.example.com/", "/uploads/a.jpg")]
    [InlineData("https://cdn.example.com", "uploads/a.jpg")]
    [InlineData("https://cdn.example.com/", "uploads/a.jpg")]
    public void SlashesAreNotDoubledOrDropped(string baseUrl, string stored)
    {
        Assert.Equal("https://cdn.example.com/uploads/a.jpg", Build(baseUrl).Resolve(stored));
    }
}
