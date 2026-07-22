using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace KHDMA.Infrastructure.Middleware;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;

    public AuditMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUnitOfWork unitOfWork)
    {
        await _next(context);

        // Log only Admin actions (PUT, POST, DELETE)
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "";
        var isAdminPath = path.Contains("/api/admin");
        var isWriteMethod = method is "POST" or "PUT" or "DELETE";

        if (isAdminPath && isWriteMethod && context.Response.StatusCode < 400)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId is not null)
            {
                var log = new AuditLog
                {
                    UserId = userId,
                    Action = method,
                    Target = path,
                    Timestamp = DateTime.UtcNow,
                    StatusCode = context.Response.StatusCode
                };

                await unitOfWork.Repository<AuditLog>().CreateAsync(log);
                await unitOfWork.CommitAsync();
            }
        }
    }
}