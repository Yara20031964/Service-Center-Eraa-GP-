using System.Text.Json;
using Domain.Common;

namespace KHDMA.API.Middleware;

/// <summary>
/// Last line of defence for the response contract: without it an unhandled
/// exception leaves the pipeline as a bodiless 500 (or, in Development, an HTML
/// stack trace), which is the one case where a caller cannot parse
/// <c>ApiResponse</c> out of the body.
/// </summary>
public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

            // Nothing can be done once the status line is on the wire - rewriting
            // the body here would corrupt a partially streamed response.
            if (context.Response.HasStarted) throw;

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            // The exception message is only echoed outside Production; it can carry
            // connection strings and SQL fragments.
            var body = ApiResponse<object>.ServerError(
                _env.IsProduction() ? "An unexpected error occurred" : ex.Message);

            await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
