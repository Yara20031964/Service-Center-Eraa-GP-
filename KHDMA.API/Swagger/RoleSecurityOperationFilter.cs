using Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace KHDMA.API.Swagger;

/// <summary>
/// Points every operation at the security scheme for the role that calls it, so
/// Swagger UI sends the Admin token to admin routes and the Provider token to
/// provider routes without anything being swapped between calls.
/// </summary>
/// <remarks>
/// A single shared "Bearer" definition is why one token at a time was the only
/// option: Swagger UI keys a stored credential by scheme name, so one name means
/// one value for the whole page. Four names means four values held at once, and
/// the operation's own <c>security</c> block decides which is attached.
/// </remarks>
public sealed class RoleSecurityOperationFilter : IOperationFilter
{
    /// <summary>Scheme names - these are the labels on the Authorize dialog.</summary>
    public const string Admin    = "AdminBearer";
    public const string Customer = "CustomerBearer";
    public const string Provider = "ProviderBearer";
    public const string Common   = "CommonBearer";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        // No padlock on routes that take no token: the public catalogue, plus
        // login and register. AllowAnonymous wins over a controller-level
        // [Authorize], which mirrors how the framework itself resolves them.
        var requiresAuth = metadata.OfType<IAuthorizeData>().Any()
                           && !metadata.OfType<IAllowAnonymous>().Any();
        if (!requiresAuth) return;

        var scheme = SchemeFor(ApiTags.SelectFor(context.ApiDescription).FirstOrDefault());

        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = scheme,
                        },
                    },
                    Array.Empty<string>()
                }
            }
        };

        var apiResponseSchema = context.SchemaGenerator.GenerateSchema(
            typeof(ApiResponse<object>), context.SchemaRepository);

        operation.Responses.TryAdd("401", JsonResponse(
            "Missing or invalid token", apiResponseSchema));
        if (scheme != Common)
            operation.Responses.TryAdd("403", JsonResponse(
                "Token belongs to a different role", apiResponseSchema));
    }

    private static OpenApiResponse JsonResponse(string description, OpenApiSchema schema) => new()
    {
        Description = description,
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new() { Schema = schema },
        },
    };

    // The tag already encodes the audience, so the section a route was filed under
    // is also the answer to which token it needs - no second mapping to maintain.
    private static string SchemeFor(string? tag) => tag switch
    {
        not null when tag.Contains("ADMIN")    => Admin,
        not null when tag.Contains("CUSTOMER") => Customer,
        not null when tag.Contains("PROVIDER") => Provider,
        _ => Common,
    };
}
