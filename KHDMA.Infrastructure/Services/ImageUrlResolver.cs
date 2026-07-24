using KHDMA.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace KHDMA.Infrastructure.Services;

public class ImageUrlResolver : IImageUrlResolver
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ImageUrlResolver(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public string? Resolve(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return path;

        var baseUrl = _configuration["App:PublicBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request is null || !request.Host.HasValue)
                return path;

            baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";
        }

        return $"{baseUrl.Trim().TrimEnd('/')}/{path.TrimStart('/')}";
    }
}
