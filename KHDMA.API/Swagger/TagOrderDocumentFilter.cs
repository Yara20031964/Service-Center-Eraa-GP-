using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace KHDMA.API.Swagger;

/// <summary>
/// Writes the document-level <c>tags</c> array so Swagger UI renders the sections
/// in role order (Admin -> Customer -> Provider -> Common -> Public) instead of
/// the order the routes happen to be discovered in, and gives each section the
/// blurb from <see cref="ApiTags.Ordered"/>.
/// </summary>
public sealed class TagOrderDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var inUse = swaggerDoc.Paths.Values
            .SelectMany(path => path.Operations.Values)
            .SelectMany(operation => operation.Tags)
            .Select(tag => tag.Name)
            .ToHashSet(StringComparer.Ordinal);

        var ordered = ApiTags.Ordered
            .Where(t => inUse.Contains(t.Tag))
            .Select(t => new OpenApiTag { Name = t.Tag, Description = t.Description })
            .ToList();

        // A controller added later without a [Tags] attribute still has to appear,
        // or its endpoints would silently vanish from the UI. Park it at the end
        // with a note saying what to do about it.
        var known = ApiTags.Ordered.Select(t => t.Tag).ToHashSet(StringComparer.Ordinal);
        ordered.AddRange(inUse
            .Where(name => !known.Contains(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .Select(name => new OpenApiTag
            {
                Name = name,
                Description = "_Untagged._ Add `[Tags(ApiTags.X)]` to this controller to file it under a role.",
            }));

        swaggerDoc.Tags = ordered;
    }
}
